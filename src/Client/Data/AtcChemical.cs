namespace AtcDemo.Shared;

using System.Web;

public partial class AtcChemical
{
    public string Url => $"https://www.atccode.com/{HttpUtility.UrlEncode(Code)}";
}
