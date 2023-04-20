#pragma warning disable SA1600
#pragma warning disable SA1402 // File may only contain a single type
namespace MultiTenantConsoleAppTest
{
    using System.IO;
    using Exos.Platform.TenancyHelper.Models;

    public static class Program
    {
        public static void Main(string[] args)
        {
            var path = Path.Combine("..\\.." + Directory.GetCurrentDirectory(), "\\MyDocumentPolicy.json");
            string policyDoc = File.ReadAllText(@"C:\Users\954\source\repos\MultiTenantLib\MultitenantConsoleAppTest\MyDocumentPolicy.json");

            // IPolicyContext policyCtx = new PolicyContext(new UserContext());

            // policyCtx.SetCustomIntField("MasterClientProfileId", 3423);

            // policyCtx.SetCustomIntField("SubcontractorTenantId", 2);

            // new up Policy helper and Policy context.
            // PolicyHelper objPolicyHelper = new PolicyHelper(null, policyCtx);
            // PolicyHelper objPolicyHelper = new PolicyHelper(PolicyDoc, policyCtx);

            // Get the obkect which will be inserted.
            VendorProfile vendorProfile = new VendorProfile() { VendorDetails = "Abc Appraisals", VendorProfileId = 1, };
            // vendorProfile.Tenant = new TenantModel();
            // objPolicyHelper.SetTenantIdsForInsert(vendorProfile);
            // var parameters = new SqlParameterCollection();
            // objPolicyHelper.GetCosmosWhereClause("vp.", parameters, null);
        }
    }

    public class VendorProfile : BaseModel
    {
        public int VendorProfileId { get; set; }

        public string VendorDetails { get; set; }

        public int ServicerId { get; set; }

        public int SubClient { get; set; }

        public int Vendor { get; set; }

        public int SubContractor { get; set; }

        public int Servicer { get; set; }

        public int ServicerGroup { get; set; }

        public override string CosmosDocType => "VendorProfile";
        /*Rest of the fields....*/
    }
}
#pragma warning restore SA1600
#pragma warning restore SA1402 // File may only contain a single type
