using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;

namespace DayPlannerApp.Views;

public partial class MarkdownDisplayControl : UserControl
{
    public static readonly DependencyProperty MarkdownTextProperty =
        DependencyProperty.Register(
            nameof(MarkdownText),
            typeof(string),
            typeof(MarkdownDisplayControl),
            new PropertyMetadata(string.Empty, OnMarkdownTextChanged));

    public MarkdownDisplayControl()
    {
        InitializeComponent();
    }

    public string MarkdownText
    {
        get => (string)GetValue(MarkdownTextProperty);
        set => SetValue(MarkdownTextProperty, value);
    }

    private static void OnMarkdownTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is MarkdownDisplayControl control)
        {
            control.RenderMarkdown(e.NewValue as string ?? string.Empty);
        }
    }

    private void RenderMarkdown(string markdownText)
    {
        MarkdownDocument.Blocks.Clear();

        if (string.IsNullOrWhiteSpace(markdownText))
        {
            MarkdownDocument.Blocks.Add(new Paragraph(new Run("No content")));
            return;
        }

        // Simple markdown rendering - in production, use MarkdownProcessor service
        // For now, just display as plain text with basic formatting
        var paragraph = new Paragraph(new Run(markdownText))
        {
            TextAlignment = TextAlignment.Left
        };
        
        MarkdownDocument.Blocks.Add(paragraph);
    }
}
