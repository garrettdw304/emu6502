using System.Text;

namespace Emu6502Gui
{
    internal class AnsiStateMachine
    {
        private const int COLUMNS = 40;
        private const int ROWS = 30;

        private static readonly byte[] PARAM_BYTES_ACCEPTED = "0123456789:;"u8.ToArray();
        private static readonly byte[] PARAM_VALUE_BYTES_ACCEPTED = "0123456789"u8.ToArray();
        private static readonly byte[] FINAL_BYTES_ACCEPTED = "ABCDHJn"u8.ToArray();

        /// <summary>
        /// The final destination of the graphics output of this terminal.
        /// </summary>
        private readonly Graphics graphics;
        /// <summary>
        /// Contains the output of the most recent output-generating ansi command. When a command generates output, previous output is cleared if not already read.
        /// </summary>
        private readonly Queue<byte> output = new Queue<byte>();
        private readonly Bitmap[] font = new Bitmap[256];
        /// <summary>
        /// Holds 30 bitmaps of size 320x8, one for each row.
        /// </summary>
        private readonly Bitmap[] rows = new Bitmap[ROWS];
        private readonly Graphics[] rowsG = new Graphics[ROWS];
        /// <summary>
        /// The index of the row that will be drawn at the top of the screen. When a new line occurs, this can be incremented to cause all rows to be shifted up by one.
        /// </summary>
        private int rowShift = 0;

        private int column = 0;
        private int row = 0;

        private State state = State.IDLE;
        private readonly StringBuilder parameterBytes = new StringBuilder();

        public AnsiStateMachine(Graphics graphics, Bitmap fontImage)
        {
            this.graphics = graphics;

            for (int i = 0; i < rows.Length; i++)
                (rowsG[i] = Graphics.FromImage(rows[i] = new Bitmap(320, 8))).Clear(Color.Black);

            for (int i = 0; i < font.Length; i++)
                font[i] = fontImage.Clone(new Rectangle((i % 16) * 8, (i / 16) * 8, 8, 8), fontImage.PixelFormat);
        }

        public void Write(byte c)
        {
            switch (state)
            {
                case State.IDLE:
                    Idle(c);
                    break;
                case State.ESCAPED:
                    Escaped(c);
                    break;
                case State.CSI:
                    Csi(c);
                    break;
            }
        }
        
        public byte Read()
        {
            if (!output.TryDequeue(out byte result))
                result = 0;

            return result;
        }

        public bool ReadAvailable()
        {
            return output.Count > 0;
        }

        // Public to allow the redrawing of rows if something is drawn overtop the terminal and the terminal needs to be on top again (like switching from text to sprite and back to text mode)
        public void RedrawRows()
        {
            for (int i = 0; i < ROWS; i++)
                graphics.DrawImage(GetRowB(i), 0, i * 8);
        }

        private void IncColumn()
        {
            column++;
            if (column >= COLUMNS)
            {
                column = 0;
                IncRow();
            }
        }

        private void IncRow()
        {
            row++;
            if (row >= ROWS) // Adding a new row to the bottom of the screen. We need to clear that new row.
            {
                row = ROWS - 1;
                rowShift = (rowShift + 1) % ROWS;
                GetRowG(row).Clear(Color.Black);
                RedrawRows();
            }
        }

        private Bitmap GetRowB(int row)
        {
            return rows[(row + rowShift) % ROWS];
        }

        private Graphics GetRowG(int row)
        {
            return rowsG[(row + rowShift) % ROWS];
        }

        private enum State
        {
            /// <summary>
            /// Waiting for the escape character 0x1B.
            /// </summary>
            IDLE,
            /// <summary>
            /// Have already received an escape character 0x1B. Waiting for an opening square bracket to indicate a CSI.
            /// </summary>
            ESCAPED,
            /// <summary>
            /// Collecting parameter bytes of the control sequence. These bytes include 0–9:;
            /// We are waiting to see an invalid byte or final byte. Final bytes include @A–Z[\]^_`a–o
            /// </summary>
            CSI,
        }

        private void Idle(byte input)
        {
            if (input == 0x1B)
            {
                state = State.ESCAPED;
                return;
            }

            if (input == '\r')
                column = 0;
            else if (input == '\n')
                IncRow();
            else if (input == 8) // Backspace
                column = column - 1 < 0 ? 0 : column - 1;
            else
            {
                graphics.DrawImage(font[input], column * 8, row * 8);
                GetRowG(row).DrawImage(font[input], column * 8, 0);
                IncColumn();
            }
        }

        private void Escaped(byte input)
        {
            if (input == (byte)'[')
            {
                parameterBytes.Clear();
                state = State.CSI;
                return;
            }

            state = State.IDLE;
            Idle(input);
        }

        private void Csi(byte input)
        {
            if (FINAL_BYTES_ACCEPTED.Contains(input))
            {
                HandleCommand(input);
                state = State.IDLE;
            }
            else if (PARAM_BYTES_ACCEPTED.Contains(input))
                parameterBytes.Append((char)input);
            else
            {
                state = State.IDLE;
                Idle(input);
            }
        }

        private void HandleCommand(byte finalByte)
        {
            int[]? parameters;
            switch (finalByte)
            {
                case (byte)'A':
                    parameters = ParseSemiSeparatedParams(parameterBytes.ToString(), 1, 1);
                    if (parameters != null)
                    {
                        row -= parameters[0];
                        if (row < 0)
                            row = 0;
                    }
                    break;
                case (byte)'B':
                    parameters = ParseSemiSeparatedParams(parameterBytes.ToString(), 1, 1);
                    if (parameters != null)
                    {
                        row += parameters[0];
                        if (row >= ROWS)
                            row = ROWS - 1;
                    }
                    break;
                case (byte)'C':
                    parameters = ParseSemiSeparatedParams(parameterBytes.ToString(), 1, 1);
                    if (parameters != null)
                    {
                        column += parameters[0];
                        if (column >= COLUMNS)
                            column = COLUMNS - 1;
                    }
                    break;
                case (byte)'D':
                    parameters = ParseSemiSeparatedParams(parameterBytes.ToString(), 1, 1);
                    if (parameters != null)
                    {
                        column -= parameters[0];
                        if (column < 0)
                            column = 0;
                    }
                    break;
                case (byte)'H':
                    parameters = ParseSemiSeparatedParams(parameterBytes.ToString(), 1, 0, 2);
                    if (parameters != null)
                    {
                        if (parameters.Length == 0)
                            row = column = 0;
                        else
                        {
                            row = parameters[0] - 1; // -1 because parameter is 1 based and index is 0 based
                            column = parameters[1] - 1; // -1 because parameter is 1 based and index is 0 based
                            if (row < 0) row = 0;
                            if (row >= ROWS) row = ROWS - 1;
                            if (column < 0) column = 0;
                            if (column >= COLUMNS) column = COLUMNS - 1;
                        }
                    }
                    break;
                case (byte)'J':
                    parameters = ParseSemiSeparatedParams(parameterBytes.ToString(), 0, 1);
                    if (parameters != null)
                    {
                        if (parameters[0] == 0)
                            throw new NotImplementedException();
                        else if (parameters[0] == 1)
                            throw new NotImplementedException();
                        else if (parameters[0] == 2)
                        {
                            graphics.Clear(Color.Black);
                            foreach (var r in rowsG) r.Clear(Color.Black);
                        }
                    }
                    break;
                case (byte)'n':
                    parameters = ParseSemiSeparatedParams(parameterBytes.ToString(), 1, 1);
                    if (parameters != null && parameters[0] == 6)
                        throw new NotImplementedException();
                    break;
            }

            state = State.IDLE;
        }

        /// <summary>
        /// Parses parameters seperated by semicolons. Returns null if there is an invalid character or format.
        /// </summary>
        private static int[]? ParseSemiSeparatedParams(string parameters, int defaultValue, params int[] expectedParams)
        {
            string[] separated = parameters.Split(';');
            if (!expectedParams.Contains(separated.Length))
            {
                if (!expectedParams.Contains(0) || separated[0].Length != 0)
                    return null;
                else
                    separated = [];
            }

            int[] toReturn = new int[separated.Length];
            for (int i = 0; i < separated.Length; i++)
            {
                if (separated[i].Length == 0)
                    toReturn[i] = defaultValue;
                else if (!separated[i].All((c) => PARAM_VALUE_BYTES_ACCEPTED.Contains((byte)c)) || !int.TryParse(separated[i], out toReturn[i]))
                    return null;
            }

            return toReturn;
        }
    }
}
