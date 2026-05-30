using PdfiumViewer;
using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Linq;
using Tesseract;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace Lab2
{
    public partial class Form1 : Form
    {
        public class openedFileName
        {
            private readonly Form1 form;
            private string fileName = "";
            public openedFileName(Form1 form)
            {
                this.form = form;
            }

            public bool isEmpty()
            {
                return fileName.Length == 0;
            }
            public bool isPDF()
            {
                return fileName.EndsWith(".pdf") || fileName.EndsWith(".PDF");
            }
            public bool isPNG()
            {
                return fileName.EndsWith(".png") || fileName.EndsWith(".PNG");
            }
            public string get()
            {
                return fileName;
            }
            public void set(string fileName)
            {
                this.fileName = fileName;
                form.Text = isEmpty() ? APPLICATION_NAME : string.Format("{0} ({1})", APPLICATION_NAME, fileName);
            }
        }

        private const string APPLICATION_NAME = "Robex";
        private const string FILE_FILTER = "Изображение PNG (*.png)|*.png|Изображение JPEG (*.jpg)|*.jpg|Документ PDF (*.pdf)|*.pdf";

        private openedFileName fileName;

        private readonly float pictureWidth;
        private readonly float pictureHeight;

        private Stack<Operation> history = new Stack<Operation>();
        private Operation currentOperation = null;

        private PdfDocument canvasDocument;
        private Bitmap canvasImage;
        private Graphics canvasGraphics;

        private float scale;
        private PointF translation;

        private Operation.Type type = Operation.Type.NONE;
        private Brush mainColor = Brushes.Black;
        private Brush secondColor = Brushes.Gray;

        public Form1() : this("") {}
        public Form1(string openedFileName)
        {
            this.fileName = new openedFileName(this);
            this.fileName.set(openedFileName);
            this.scale = 1.0f;

            InitializeComponent();

            langBox.SelectedIndex = 0;

            openFileDialog.Filter = saveFileDialog.Filter = FILE_FILTER;
            saveFileDialog.FileName = openedFileName;
            mainColorBox.BackColor = ((SolidBrush)mainColor).Color;

            if (fileName.isEmpty())
            {
                Text = APPLICATION_NAME;
                pictureWidth = 1000;
                pictureHeight = 500;
                canvasImage = new Bitmap((int)pictureWidth, (int)pictureHeight, System.Drawing.Imaging.PixelFormat.Format32bppRgb);
                canvasGraphics = Graphics.FromImage(canvasImage);
                canvasGraphics.Clear(Color.White);
                canvas.Image = canvasImage;
            }
            else if (fileName.isPDF())
            {
                Text = string.Format("{0} ({1})", APPLICATION_NAME, openedFileName);
                canvasDocument = PdfDocument.Load(openedFileName);

                var pageSize = canvasDocument.PageSizes[0];
                int dpi = 150;
                int width = (int)(pageSize.Width * dpi / 72);
                int height = (int)(pageSize.Height * dpi / 72);

                using (var renderer = canvasDocument.Render(0, dpi, dpi, PdfRenderFlags.ForPrinting))
                {
                    canvasImage = new Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
                    pictureWidth = canvasImage.Width;
                    pictureHeight = canvasImage.Height;

                    using (Graphics g = Graphics.FromImage(canvasImage))
                    {
                        g.Clear(Color.White);
                        g.DrawImage(renderer, 0, 0);
                    }
                }
            }
            else
            {
                Text = string.Format("{0} ({1})", APPLICATION_NAME, openedFileName);
                using (Image image = Image.FromFile(openedFileName))
                {
                    pictureWidth = image.Width;
                    pictureHeight = image.Height;
                    canvasImage = new Bitmap(image.Width, image.Height, System.Drawing.Imaging.PixelFormat.Format32bppRgb);
                    canvasGraphics = Graphics.FromImage(canvasImage);
                    canvasGraphics.DrawImage(image, 0, 0);
                }
                canvas.Image = canvasImage;
                recognizeEvent();
            }

            canvasGraphics.SmoothingMode = SmoothingMode.AntiAlias;
            canvasGraphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            updateCanvasState();
        }
        private void openWindow()
        {
            Thread thread = new Thread(() =>
            {
                Form1 form = new Form1();
                form.Show();

                System.Windows.Forms.Application.Run(form);
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
        }
        private void openWindow(string fileName)
        {
            Thread thread = new Thread(() =>
            {
                Form1 form = new Form1(fileName);
                form.Show();

                Application.Run(form);
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
        }


        private void canvasChangeScale(object sender, MouseEventArgs e)
        {
            if (scale <= 0.75 && e.Delta > 0)
                return;

            if (scale >= 4 && e.Delta < 0)
                return;

            scale += e.Delta * -0.0003f;
            statusScale.Text = $"{Math.Round(scale * 100)}%";

            updateCanvasState();
        }
        private void canvasChangeSize(object sender, EventArgs e)
        {
            updateCanvasState();
        }
        private void canvasMove(PointF start, PointF end)
        {
            canvas.Location = new Point(
                Convert.ToInt32((end.X - start.X) + canvas.Location.X),
                Convert.ToInt32((end.Y - start.Y) + canvas.Location.Y));
            translation = new PointF(
                (end.X - start.X) / scale + translation.X,
                (end.Y - start.Y) / scale + translation.Y);
        }

        private void updateCanvasState()
        {
            canvas.Width = Convert.ToInt32(scale * pictureWidth);
            canvas.Height = Convert.ToInt32(scale * pictureHeight);

            canvas.Location = new Point(
                (canvasPanel.Width - canvas.Width) / 2 + (int)(translation.X * scale),
                (canvasPanel.Height - canvas.Height) / 2 + (int)(translation.Y * scale));
        }
        private void updateCanvasContent()
        {
            canvas.Update();
        }

        private bool isLeftMouseDown = false;

        private bool isRightMouseDown = false;
        private PointF lastRightMouseDownPoint = default;

        private Cursor cursorBeforeMoveCanvas;
        private void mouseMoveEvent(object sender, MouseEventArgs e)
        {
            PointF mouse = new PointF(e.Location.X / scale, e.Location.Y / scale);
            if (isRightMouseDown)
            {
                canvasMove(lastRightMouseDownPoint, mouse);
                return; 
            }
            if (isLeftMouseDown)
            {
                if (currentOperation == null)
                    return;

                currentOperation.move(new PointF(e.Location.X / scale, e.Location.Y / scale));
                canvas.Refresh();
                return;
            }
        }
        private void mouseDownEvent(object sender, MouseEventArgs e)
        {

            PointF mouse = new PointF(e.Location.X / scale, e.Location.Y / scale);
            switch (e.Button)
            {
                case MouseButtons.Left:
                    {
                        if (isLeftMouseDown)
                            return;

                        isLeftMouseDown = true;
                        float width = type.Equals(Operation.Type.ERASER) ? 20 : 5;
                        currentOperation = Operation.of(type, canvasGraphics, canvasImage, mainColor, secondColor, width);
                        if (currentOperation == null)
                            return; 
                        currentOperation.down(mouse);
                    }
                    break;
                case MouseButtons.Right:
                    {
                        if (isRightMouseDown)
                            return;

                        cursorBeforeMoveCanvas = canvas.Cursor;
                        canvas.Cursor = Cursors.Hand;

                        lastRightMouseDownPoint = mouse;
                        isRightMouseDown = true;
                    }
                    break;
            }
        }
        private void mouseUpEvent(object sender, MouseEventArgs e)
        {
            PointF mouse = new PointF(e.Location.X / scale, e.Location.Y / scale);
            switch (e.Button)
            {
                case MouseButtons.Left:
                    {
                        if (!isLeftMouseDown)
                            return;

                        isLeftMouseDown = false;

                        if (type.Equals(Operation.Type.NONE))
                            return;

                        if (currentOperation == null)
                            return;

                        currentOperation.up(mouse);
                        currentOperation = null;

                        canvas.Refresh();
                        recognizeEvent();
                    } break;
                case MouseButtons.Right:
                    {
                        if (!isRightMouseDown)
                            return;

                        canvas.Cursor = cursorBeforeMoveCanvas;
                        lastRightMouseDownPoint = default;
                        isRightMouseDown = false;
                    } break;
            }  
        }


        private void createFileEvent(object sender, EventArgs e)
        {
            openWindow();
        }        
        private void openFileEvent(object sender, EventArgs e)
        {
            if (openFileDialog.ShowDialog(this) == DialogResult.Cancel)
                return;

            openWindow(openFileDialog.FileName);
        }
        private void saveFileEvent(object sender, EventArgs e)
        {
            if (!fileName.isEmpty())
            {
                canvasImage.Save(fileName.get(), fileName.get().EndsWith(".png") ? System.Drawing.Imaging.ImageFormat.Png : System.Drawing.Imaging.ImageFormat.Jpeg);
                return;
            }

            saveAsFileEvent(sender, e);
        }
        private void saveAsFileEvent(object sender, EventArgs e)
        {
            if (saveFileDialog.ShowDialog(this) == DialogResult.Cancel)
                return;

            fileName.set(saveFileDialog.FileName);
            canvasImage.Save(fileName.get(), fileName.get().EndsWith(".png") ? System.Drawing.Imaging.ImageFormat.Png : System.Drawing.Imaging.ImageFormat.Jpeg);
        }
        private void closeEvent(object sender, EventArgs e)
        {
            this.Close();
        }

        private void RollerClickEvent(object sender, EventArgs e)
        {
            switch (type)
            {
                case Operation.Type.ROLLER:
                    {
                        type = Operation.Type.NONE;
                        canvas.Cursor = Cursors.Arrow;
                        buttonRoller.FlatAppearance.BorderSize = 0;
                        rollerToolStripMenuItem.Checked = false;
                    }
                    return;
                default:
                    {
                        type = Operation.Type.ROLLER;
                        hideBorderAllButtons();
                        canvas.Cursor = Cursors.Cross;
                        buttonRoller.FlatAppearance.BorderSize = 5;
                        rollerToolStripMenuItem.Checked = false;
                    }
                    return;
            }
        }

        private void EraserClickEvent(object sender, EventArgs e)
        {
            switch (type)
            {
                case Operation.Type.ERASER:
                    {
                        type = Operation.Type.NONE;
                        canvas.Cursor = Cursors.Arrow;
                        buttonEraser.FlatAppearance.BorderSize = 0;
                        eraserToolStripMenuItem.Checked = false;
                    }
                    return;
                default:
                    {
                        type = Operation.Type.ERASER;
                        hideBorderAllButtons();
                        canvas.Cursor = Cursors.Cross;
                        buttonEraser.FlatAppearance.BorderSize = 5;
                        eraserToolStripMenuItem.Checked = true;
                    }
                    return;
            }
        }

        private void hideBorderAllButtons()
        {
            buttonRoller.FlatAppearance.BorderSize = 0;
            buttonEraser.FlatAppearance.BorderSize = 0;

            rollerToolStripMenuItem.Checked = false;
            eraserToolStripMenuItem.Checked = false;
        }

        private void PalleteMainClickEvent(object sender, EventArgs e)
        {
            DialogResult result = colorDialog.ShowDialog();
            if (!result.Equals(DialogResult.OK))
                return;

            mainColorBox.BackColor = colorDialog.Color;
            mainColor = new SolidBrush(colorDialog.Color);
        }

        private void showInformationEvent(object sender, EventArgs e)
        {
            new Form2().ShowDialog();
        }



        private async void recognizeEvent()
        {
            string lang = langBox.Text.ToLower();
            if (fileName.isEmpty() || fileName.isPNG())
            {
                string result = await Task.Run(() => recognizeBitmap(canvasImage, lang));
                textResult.Text = result;
            }
            else if (fileName.isPDF())
            {
                textResult.Text = recognizePDF(lang);
            }

        }
        private string recognizePDF(string lang)
        {
            int pageCount = canvasDocument.PageCount;
            string result = "";
            for (int i = 0; i < pageCount; i++)
            {
                using (Bitmap bitmap = renderPDFPageToBitmap(i))
                {

                    string pageText = recognizeBitmap(bitmap, lang);

                    if (!string.IsNullOrWhiteSpace(pageText))
                    {
                        result += $"--- Страница {i + 1} ---\n";
                        result += pageText + "\n\n";
                    }
                }
            }
            return result;
        }
        private int pdfDPI = 150;
        private Bitmap renderPDFPageToBitmap(int index)
        {
            var pageSize = canvasDocument.PageSizes[index];

            int width = (int)(pageSize.Width * pdfDPI / 72);
            int height = (int)(pageSize.Height * pdfDPI / 72);

            using (var renderer = canvasDocument.Render(index, pdfDPI, pdfDPI, PdfRenderFlags.ForPrinting))
            {
                Bitmap bmp = new Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
                using (Graphics g = Graphics.FromImage(bmp))
                {
                    g.Clear(Color.White);
                    g.DrawImage(renderer, 0, 0, width, height);
                }

                return bmp;
            }
        }

        private string recognizeBitmap(Bitmap bmp, string lang)
        {
            string dataPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tessdata");
            using (TesseractEngine engine = new TesseractEngine(dataPath, lang, EngineMode.TesseractOnly))
            {
                engine.SetVariable("tessedit_char_whitelist", "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyzАБВГДЕЁЖЗИЙКЛМНОПРСТУФХЦЧШЩЪЫЬЭЮЯабвгдеёжзийклмнопрстуфхцчшщъыьэюя0123456789.,-!?;:()@#$%&*+= ");

                string text;
                using (var pix = PixConverter.ToPix(bmp))
                using (var page = engine.Process(pix))
                {
                    text = page.GetText();
                    return string.IsNullOrWhiteSpace(text) ? "(Пусто или не распознано)" : text;
                }
            }
        }
    }
}
