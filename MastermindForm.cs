using System.Drawing.Drawing2D;

namespace Mastermind;

internal sealed class MastermindForm : Form
{
    private readonly GameConfiguration configuration;
    private readonly CheatCodeStore cheatCodeStore;
    private readonly FileAttemptLogger attemptLogger;
    private readonly TextBox cheatCodeInput = new();
    private readonly ComboBox codeLengthSelector = new();
    private readonly Font titleFont = new("Segoe UI", 24, FontStyle.Bold);
    private readonly Font bodyFont = new("Segoe UI", 10, FontStyle.Regular);
    private readonly Font smallFont = new("Segoe UI", 8, FontStyle.Bold);
    private readonly Font buttonFont = new("Segoe UI", 9, FontStyle.Bold);
    private GameEngine game;
    private int selectedPosition = -1;
    private string statusMessage = "Selecciona un espacio y asigna colores distintos.";
    private bool statusIsError;

    public MastermindForm()
    {
        string appDirectory = AppContext.BaseDirectory;
        configuration = GameConfiguration.LoadOrCreate(Path.Combine(appDirectory, "game-config.json"));
        cheatCodeStore = new CheatCodeStore(Path.Combine(appDirectory, "cheat-codes.json"));
        attemptLogger = new FileAttemptLogger(Path.Combine(appDirectory, "attempts.log"));
        game = new GameEngine(configuration, cheatCodeStore, attemptLogger);

        Text = "Mastermind";
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(1040, 820);
        MinimumSize = new Size(900, 720);
        BackColor = Color.FromArgb(17, 23, 27);
        ForeColor = Color.FromArgb(231, 234, 225);
        DoubleBuffered = true;
        Font = bodyFont;

        ConfigureHeaderControls();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        Graphics graphics = e.Graphics;
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

        using var backgroundBrush = new SolidBrush(BackColor);
        graphics.FillRectangle(backgroundBrush, ClientRectangle);
        DrawHeader(graphics);
        DrawBoard(graphics);
        DrawPalette(graphics);
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        if (e.Button != MouseButtons.Left)
        {
            return;
        }

        if (GetNewGameButton().Contains(e.Location))
        {
            StartNewGame();
            return;
        }

        if (GetSubmitButton().Contains(e.Location))
        {
            SubmitGuess();
            return;
        }

        if (GetClearButton().Contains(e.Location))
        {
            if (selectedPosition < 0)
            {
                SetStatus("Selecciona primero el espacio que quieres borrar.", true);
            }
            else
            {
                game.ClearColor(selectedPosition);
                SetStatus("Espacio borrado.");
            }

            Invalidate();
            return;
        }

        for (int position = 0; position < game.CodeLength; position++)
        {
            if (!GetHoleBounds(position).Contains(e.Location) || game.IsOver)
            {
                continue;
            }

            selectedPosition = position;
            SetStatus($"Espacio {position + 1} seleccionado. Ahora elige un color.");
            Invalidate();
            return;
        }

        for (int index = 0; index < configuration.Colores.Count; index++)
        {
            if (!GetPaletteBounds(index).Contains(e.Location))
            {
                continue;
            }

            AssignColor(configuration.Colores[index].Codigo[0]);
            return;
        }
    }

    private void ConfigureHeaderControls()
    {
        cheatCodeInput.Location = new Point(385, 45);
        cheatCodeInput.Size = new Size(158, 28);
        cheatCodeInput.PlaceholderText = "Introduce un código";
        cheatCodeInput.BackColor = Color.FromArgb(26, 36, 38);
        cheatCodeInput.ForeColor = ForeColor;
        cheatCodeInput.BorderStyle = BorderStyle.FixedSingle;
        Controls.Add(cheatCodeInput);

        codeLengthSelector.Location = new Point(582, 44);
        codeLengthSelector.Size = new Size(82, 28);
        codeLengthSelector.DropDownStyle = ComboBoxStyle.DropDownList;
        codeLengthSelector.FlatStyle = FlatStyle.Flat;
        codeLengthSelector.BackColor = Color.FromArgb(26, 36, 38);
        codeLengthSelector.ForeColor = ForeColor;
        foreach (int size in configuration.TamanosPermitidos)
        {
            codeLengthSelector.Items.Add(size);
        }

        codeLengthSelector.SelectedItem = game.CodeLength;
        codeLengthSelector.SelectedIndexChanged += (_, _) =>
        {
            if (codeLengthSelector.SelectedItem is int selectedSize && selectedSize != game.CodeLength)
            {
                StartNewGame(selectedSize);
            }
        };
        Controls.Add(codeLengthSelector);
    }

    private void DrawHeader(Graphics graphics)
    {
        using var accentBrush = new SolidBrush(Color.FromArgb(91, 204, 139));
        using var mutedBrush = new SolidBrush(Color.FromArgb(144, 158, 155));
        using var titleBrush = new SolidBrush(ForeColor);
        using var linePen = new Pen(Color.FromArgb(47, 60, 62));

        graphics.FillEllipse(accentBrush, 40, 39, 10, 10);
        graphics.DrawString("MASTERMIND", titleFont, titleBrush, 60, 24);
        graphics.DrawString("DESCIFRA EL CÓDIGO", smallFont, mutedBrush, 62, 65);
        graphics.DrawString("CHEAT CODE · SE CONSUME AL PROBAR", smallFont, mutedBrush, 385, 26);
        graphics.DrawString("TAMAÑO", smallFont, mutedBrush, 582, 26);
        DrawButton(graphics, GetNewGameButton(), "NUEVA PARTIDA", filled: false);
        graphics.DrawLine(linePen, 40, 104, ClientSize.Width - 40, 104);
    }

    private void DrawBoard(Graphics graphics)
    {
        using var mutedBrush = new SolidBrush(Color.FromArgb(144, 158, 155));
        using var activeBrush = new SolidBrush(Color.FromArgb(27, 38, 39));
        using var highlightPen = new Pen(Color.FromArgb(91, 204, 139), 2);

        int boardTop = 166;
        int paletteTop = ClientSize.Height - 164;
        int rowHeight = Math.Min(45, (paletteTop - boardTop - 30) / GameEngine.MaximumAttempts);
        int guessX = 64;
        int hintX = Math.Max(guessX + 400, ClientSize.Width - 250);

        graphics.DrawString("TU COMBINACIÓN", smallFont, mutedBrush, guessX, 132);
        graphics.DrawString("INDICIOS", smallFont, mutedBrush, hintX, 132);

        for (int row = 0; row < GameEngine.MaximumAttempts; row++)
        {
            int y = boardTop + row * rowHeight;
            if (row == game.History.Count && !game.IsOver)
            {
                Rectangle rowBounds = new(guessX - 18, y + 2, ClientSize.Width - guessX - 28, rowHeight - 4);
                graphics.FillRectangle(activeBrush, rowBounds);
                graphics.DrawRectangle(highlightPen, rowBounds);
            }

            if (row < game.History.Count)
            {
                GuessResult result = game.History[row];
                for (int position = 0; position < game.CodeLength; position++)
                {
                    DrawHole(graphics, GetHoleCenter(position, y), result.Attempt[position], isSelected: false);
                }

                for (int hint = 0; hint < game.CodeLength; hint++)
                {
                    Color? hintColor = hint < result.Evaluation.BienPosicionados
                        ? Color.FromArgb(91, 204, 139)
                        : hint < result.Evaluation.BienPosicionados + result.Evaluation.ColorCorrecto
                            ? Color.FromArgb(246, 198, 75)
                            : null;
                    DrawHint(graphics, GetHintCenter(hint, y), hintColor);
                }
            }
            else if (row == game.History.Count && !game.IsOver)
            {
                for (int position = 0; position < game.CodeLength; position++)
                {
                    DrawHole(graphics, GetHoleCenter(position, y), game.CurrentGuess[position], selectedPosition == position);
                }

                for (int hint = 0; hint < game.CodeLength; hint++)
                {
                    DrawHint(graphics, GetHintCenter(hint, y), null);
                }
            }
            else
            {
                using var emptyPen = new Pen(Color.FromArgb(46, 60, 61));
                graphics.DrawLine(emptyPen, guessX - 4, y + rowHeight, ClientSize.Width - 56, y + rowHeight);
            }
        }

        using var statusBrush = new SolidBrush(statusIsError
            ? Color.FromArgb(246, 198, 75)
            : Color.FromArgb(174, 190, 180));
        using var mutedStatusBrush = new SolidBrush(Color.FromArgb(144, 158, 155));
        int statusY = ClientSize.Height - 191;
        graphics.DrawString(statusMessage, bodyFont, statusBrush,
            new RectangleF(44, statusY, ClientSize.Width - 88, 24));
        graphics.DrawString($"{game.AttemptsRemaining} INTENTOS", smallFont, mutedStatusBrush,
            ClientSize.Width - 166, 132);
    }

    private void DrawPalette(Graphics graphics)
    {
        int top = ClientSize.Height - 155;
        using var linePen = new Pen(Color.FromArgb(47, 60, 62));
        using var labelBrush = new SolidBrush(Color.FromArgb(144, 158, 155));
        graphics.DrawLine(linePen, 40, top - 10, ClientSize.Width - 40, top - 10);
        graphics.DrawString($"PALETA · {configuration.Colores.Count} COLORES", smallFont, labelBrush, 44, top + 5);

        for (int index = 0; index < configuration.Colores.Count; index++)
        {
            ColorDefinition color = configuration.Colores[index];
            bool alreadyUsed = game.CurrentGuess
                .Where((_, position) => position != selectedPosition)
                .Contains(char.ToUpperInvariant(color.Codigo[0]));
            DrawPaletteColor(graphics, GetPaletteBounds(index), color, alreadyUsed || game.IsOver);
        }

        DrawButton(graphics, GetClearButton(), "BORRAR", filled: false);
        DrawButton(graphics, GetSubmitButton(), "PROBAR", filled: true);
    }

    private void DrawHole(Graphics graphics, Point center, char? code, bool isSelected)
    {
        int diameter = 26;
        Rectangle bounds = new(center.X - diameter / 2, center.Y - diameter / 2, diameter, diameter);
        Color fill = code.HasValue ? GetSwatchColor(code.Value) : Color.FromArgb(23, 31, 34);
        using var fillBrush = new SolidBrush(fill);
        using var outlinePen = new Pen(isSelected
            ? Color.FromArgb(91, 204, 139)
            : code.HasValue
                ? Color.FromArgb(231, 234, 225)
                : Color.FromArgb(93, 111, 109), isSelected ? 3 : 1.5f);
        graphics.FillEllipse(fillBrush, bounds);
        graphics.DrawEllipse(outlinePen, bounds);
        if (code.HasValue)
        {
            DrawCenteredText(graphics, code.Value.ToString(), bounds, smallFont,
                IsLightColor(fill) ? Color.FromArgb(17, 23, 27) : Color.White);
        }
    }

    private static void DrawHint(Graphics graphics, Point center, Color? color)
    {
        int diameter = 12;
        Rectangle bounds = new(center.X - diameter / 2, center.Y - diameter / 2, diameter, diameter);
        using var fillBrush = new SolidBrush(color ?? Color.FromArgb(27, 36, 38));
        using var outlinePen = new Pen(color ?? Color.FromArgb(57, 72, 72));
        graphics.FillEllipse(fillBrush, bounds);
        graphics.DrawEllipse(outlinePen, bounds);
    }

    private void DrawPaletteColor(Graphics graphics, Rectangle bounds, ColorDefinition definition, bool disabled)
    {
        Color color = GetSwatchColor(definition.Codigo[0]);
        using var colorBrush = new SolidBrush(disabled ? Color.FromArgb(70, color) : color);
        using var outlinePen = new Pen(disabled
            ? Color.FromArgb(61, 73, 72)
            : Color.FromArgb(214, 224, 211), disabled ? 1 : 2);
        graphics.FillEllipse(colorBrush, bounds);
        graphics.DrawEllipse(outlinePen, bounds);
        DrawCenteredText(graphics, definition.Codigo, bounds, smallFont,
            IsLightColor(color) ? Color.FromArgb(17, 23, 27) : Color.White);
    }

    private void DrawButton(Graphics graphics, Rectangle bounds, string text, bool filled)
    {
        using var fillBrush = new SolidBrush(filled
            ? Color.FromArgb(91, 204, 139)
            : Color.FromArgb(26, 36, 38));
        using var outlinePen = new Pen(filled
            ? Color.FromArgb(91, 204, 139)
            : Color.FromArgb(67, 84, 82));
        using var textBrush = new SolidBrush(filled
            ? Color.FromArgb(17, 27, 25)
            : Color.FromArgb(220, 228, 218));
        using var format = new StringFormat
        {
            Alignment = StringAlignment.Center,
            LineAlignment = StringAlignment.Center
        };

        graphics.FillRectangle(fillBrush, bounds);
        graphics.DrawRectangle(outlinePen, bounds);
        graphics.DrawString(text, buttonFont, textBrush, bounds, format);
    }

    private static void DrawCenteredText(Graphics graphics, string text, Rectangle bounds, Font font, Color color)
    {
        using var brush = new SolidBrush(color);
        using var format = new StringFormat
        {
            Alignment = StringAlignment.Center,
            LineAlignment = StringAlignment.Center
        };
        graphics.DrawString(text, font, brush, bounds, format);
    }

    private void AssignColor(char color)
    {
        if (selectedPosition < 0)
        {
            SetStatus("Selecciona primero un espacio para asignarle un color.", true);
        }
        else if (game.TrySetColor(selectedPosition, color))
        {
            SetStatus($"Color {configuration.GetColor(color).Nombre} asignado al espacio {selectedPosition + 1}.");
        }
        else
        {
            SetStatus("Ese color ya está en uso en esta combinación.", true);
        }

        Invalidate();
    }

    private void SubmitGuess()
    {
        if (game.TrySubmit(out _, out string message, cheatCodeInput.Text))
        {
            selectedPosition = -1;
            cheatCodeInput.Clear();
            SetStatus(message);
        }
        else
        {
            SetStatus(message, true);
        }

        Invalidate();
    }

    private void StartNewGame(int? codeLength = null)
    {
        game = game.CreateNextGame(codeLength);
        selectedPosition = -1;
        cheatCodeInput.Clear();
        SetStatus("Nueva partida. Selecciona un espacio y asigna colores distintos.");
        Invalidate();
    }

    private void SetStatus(string message, bool isError = false)
    {
        statusMessage = message;
        statusIsError = isError;
    }

    private Color GetSwatchColor(char code) => ColorTranslator.FromHtml(configuration.GetColor(code).Hex);

    private static bool IsLightColor(Color color) =>
        (0.299 * color.R + 0.587 * color.G + 0.114 * color.B) > 155;

    private Rectangle GetNewGameButton() => new(ClientSize.Width - 198, 37, 156, 38);
    private Rectangle GetSubmitButton() => new(ClientSize.Width - 172, ClientSize.Height - 105, 128, 46);
    private Rectangle GetClearButton() => new(ClientSize.Width - 316, ClientSize.Height - 105, 118, 46);

    private Rectangle GetPaletteBounds(int index) =>
        new(48 + index * 31, ClientSize.Height - 105, 25, 25);

    private Rectangle GetHoleBounds(int position)
    {
        int rowHeight = Math.Min(45, (ClientSize.Height - 164 - 166 - 30) / GameEngine.MaximumAttempts);
        Point center = GetHoleCenter(position, 166 + game.History.Count * rowHeight);
        return new Rectangle(center.X - 19, center.Y - 19, 38, 38);
    }

    private static Point GetHoleCenter(int position, int rowY) => new(84 + position * 56, rowY + 20);

    private Point GetHintCenter(int position, int rowY) =>
        new(Math.Max(650, ClientSize.Width - 250) + position * 32, rowY + 20);
}