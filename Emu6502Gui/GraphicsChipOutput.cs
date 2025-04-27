namespace Emu6502Gui
{
    public partial class GraphicsChipOutput : Form
    {
        private readonly Bitmap toDisplay;

        public GraphicsChipOutput(Bitmap toDisplay)
        {
            InitializeComponent();

            this.toDisplay = toDisplay;

            DoubleBuffered = true;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            float scale = Math.Min(ClientSize.Width / 320f, ClientSize.Height / 240f);
            e.Graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
            e.Graphics.TranslateTransform((ClientSize.Width - 320 * scale) / 2f, (ClientSize.Height - 240 * scale) / 2f);
            e.Graphics.ScaleTransform(scale, scale);
            e.Graphics.DrawImage(toDisplay, 0, 0);
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            Invalidate();
        }

        private void GraphicsChipOutput_Load(object sender, EventArgs e)
        {

        }
    }
}
