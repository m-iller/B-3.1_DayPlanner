using System;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using Markdig;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;

namespace DayPlannerApp.Services;

public class MarkdownProcessor : IMarkdownProcessor
{
    private readonly MarkdownPipeline _pipeline;

    public MarkdownProcessor()
    {
        _pipeline = new MarkdownPipelineBuilder()
            .UseAdvancedExtensions()
            .Build();
    }

    public FlowDocument RenderToFlowDocument(string markdownText)
    {
        if (string.IsNullOrWhiteSpace(markdownText))
        {
            return new FlowDocument();
        }

        var document = Markdown.Parse(markdownText, _pipeline);
        var flowDocument = new FlowDocument();

        foreach (var block in document)
        {
            var element = ConvertBlock(block);
            if (element != null)
            {
                flowDocument.Blocks.Add(element);
            }
        }

        return flowDocument;
    }

    public string RenderToPlainText(string markdownText)
    {
        if (string.IsNullOrWhiteSpace(markdownText))
        {
            return string.Empty;
        }

        return Markdown.ToPlainText(markdownText, _pipeline);
    }

    public bool ValidateMarkdown(string markdownText)
    {
        if (string.IsNullOrWhiteSpace(markdownText))
        {
            return true;
        }

        try
        {
            Markdown.Parse(markdownText, _pipeline);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private System.Windows.Documents.Block? ConvertBlock(Markdig.Syntax.Block block)
    {
        return block switch
        {
            HeadingBlock heading => ConvertHeading(heading),
            ParagraphBlock paragraph => ConvertParagraph(paragraph),
            ListBlock list => ConvertList(list),
            CodeBlock code => ConvertCodeBlock(code),
            _ => null
        };
    }

    private Paragraph ConvertHeading(HeadingBlock heading)
    {
        var paragraph = new Paragraph();
        var fontSize = heading.Level switch
        {
            1 => 24.0,
            2 => 20.0,
            3 => 16.0,
            4 => 14.0,
            5 => 12.0,
            _ => 10.0
        };

        paragraph.FontSize = fontSize;
        paragraph.FontWeight = FontWeights.Bold;
        paragraph.Margin = new Thickness(0, 10, 0, 5);

        AddInlines(paragraph, heading.Inline);

        return paragraph;
    }

    private Paragraph ConvertParagraph(ParagraphBlock paragraph)
    {
        var wpfParagraph = new Paragraph
        {
            Margin = new Thickness(0, 5, 0, 5)
        };

        AddInlines(wpfParagraph, paragraph.Inline);

        return wpfParagraph;
    }

    private List ConvertList(ListBlock list)
    {
        var wpfList = new List
        {
            Margin = new Thickness(0, 5, 0, 5)
        };

        if (list.IsOrdered)
        {
            wpfList.MarkerStyle = TextMarkerStyle.Decimal;
        }
        else
        {
            wpfList.MarkerStyle = TextMarkerStyle.Disc;
        }

        foreach (var item in list)
        {
            if (item is ListItemBlock listItem)
            {
                var listItemElement = new ListItem();

                foreach (var itemBlock in listItem)
                {
                    var converted = ConvertBlock(itemBlock);
                    if (converted != null)
                    {
                        listItemElement.Blocks.Add(converted);
                    }
                }

                wpfList.ListItems.Add(listItemElement);
            }
        }

        return wpfList;
    }

    private Paragraph ConvertCodeBlock(CodeBlock code)
    {
        var paragraph = new Paragraph
        {
            Background = new SolidColorBrush(Color.FromRgb(240, 240, 240)),
            Padding = new Thickness(10),
            Margin = new Thickness(0, 5, 0, 5),
            FontFamily = new FontFamily("Consolas, Courier New")
        };

        var run = new Run(code.Lines.ToString());
        paragraph.Inlines.Add(run);

        return paragraph;
    }

    private void AddInlines(Paragraph paragraph, ContainerInline? inline)
    {
        if (inline == null) return;

        foreach (var child in inline)
        {
            var element = ConvertInline(child);
            if (element != null)
            {
                paragraph.Inlines.Add(element);
            }
        }
    }

    private System.Windows.Documents.Inline? ConvertInline(Markdig.Syntax.Inlines.Inline inline)
    {
        return inline switch
        {
            LiteralInline literal => new Run(literal.Content.ToString()),
            EmphasisInline emphasis => ConvertEmphasis(emphasis),
            CodeInline code => ConvertCodeInline(code),
            LinkInline link => ConvertLink(link),
            LineBreakInline => new LineBreak(),
            _ => null
        };
    }

    private System.Windows.Documents.Inline ConvertEmphasis(EmphasisInline emphasis)
    {
        var span = new Span();

        if (emphasis.DelimiterCount == 2)
        {
            span.FontWeight = FontWeights.Bold;
        }
        else if (emphasis.DelimiterCount == 1)
        {
            span.FontStyle = FontStyles.Italic;
        }

        foreach (var child in emphasis)
        {
            var element = ConvertInline(child);
            if (element != null)
            {
                span.Inlines.Add(element);
            }
        }

        return span;
    }

    private System.Windows.Documents.Inline ConvertCodeInline(CodeInline code)
    {
        var run = new Run(code.Content)
        {
            Background = new SolidColorBrush(Color.FromRgb(240, 240, 240)),
            FontFamily = new FontFamily("Consolas, Courier New")
        };

        return run;
    }

    private System.Windows.Documents.Inline ConvertLink(LinkInline link)
    {
        var hyperlink = new Hyperlink
        {
            NavigateUri = new Uri(link.Url ?? string.Empty, UriKind.RelativeOrAbsolute)
        };

        foreach (var child in link)
        {
            var element = ConvertInline(child);
            if (element != null)
            {
                hyperlink.Inlines.Add(element);
            }
        }

        return hyperlink;
    }
}
