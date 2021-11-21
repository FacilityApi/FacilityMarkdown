namespace CodeGenCore;

public sealed class CodeGenOutputFile
{
	public CodeGenOutputFile(string name, string text)
	{
		Name = name ?? throw new ArgumentNullException(nameof(name));
		Text = text ?? throw new ArgumentNullException(nameof(text));
	}

	public string Name { get; }

	public string Text { get; }
}
