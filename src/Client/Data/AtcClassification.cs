namespace AtcDemo.Shared;

using System.Web;

public partial class AtcClassification
{
    public string AtcUrl => $"http://purl.bioontology.org/ontology/ATC/{HttpUtility.UrlEncode(Code)}";
    // TODO: 
    //public string StyUrl => $"http://purl.bioontology.org/ontology/STY/{HttpUtility.UrlEncode(StyCode)}";
}
