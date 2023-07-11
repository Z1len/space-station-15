using Robust.Shared.Prototypes;

namespace Content.Shared.RoM.Printer.Prototypes;
[Prototype("document")]
public sealed class DocumentPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; } = default!;

    ///<summary>
    ///Name of document
    ///</summary>
    [DataField("name", required:true)]
    public readonly string Name = string.Empty;

    /// <summary>
    /// Document text
    /// </summary>
    [DataField("text", required: true)]
    public readonly string Text = string.Empty;

}
