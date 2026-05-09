using System.Windows.Documents;

namespace DayPlannerApp.Services;

public interface IMarkdownProcessor
{
    FlowDocument RenderToFlowDocument(string markdownText);
    string RenderToPlainText(string markdownText);
    bool ValidateMarkdown(string markdownText);
}
