
namespace ASTools.Validator
{
    public abstract class Scanner
    {
        public abstract void Scan();

        public abstract ChecklistItem[] GetChecklistItems { get; }
    }
}
