#pragma warning disable SA1402 // File may only contain a single type
#pragma warning disable SA1649 // File name should match first type name
namespace Exos.Platform.TenancyHelper.UnitTests
{
    using System.Diagnostics.CodeAnalysis;
    using System.IO;
    using Exos.Platform.TenancyHelper.Models;
    using Microsoft.Azure.Documents;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [ExcludeFromCodeCoverage]
    public abstract class BaseModel
    {
        public abstract string CosmosDocType { get; }

        public string Id { get; set; }

        public string Version { get; set; }

        public TenantModel Tenant { get; set; }
    }

    // this derives from Base Entity(which holds all the multi tenancy fields.)
    [ExcludeFromCodeCoverage]
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

    [TestClass]
    [ExcludeFromCodeCoverage]
    public class MultiTenancyHelperTest
    {
        [TestMethod]
        public void TestPolicyHelperInsert()
        {
            // Get the policy.
            var path = Path.Combine("..\\.." + Directory.GetCurrentDirectory(), "\\MyDocumentPolicy.json");
            string policyDoc = File.ReadAllText(@"..\..\..\MyDocumentPolicy.json");

            // IPolicyContext policyCtx = new PolicyContext(new UserContext());

            // policyCtx.SetCustomIntField("MasterClientProfileId", 3423);

            // policyCtx.SetCustomIntField("SubcontractorTenantId", 2);

            // new up Policy helper and Policy context.
            // PolicyHelper objPolicyHelper = new PolicyHelper(PolicyDoc, policyCtx);

            // Get the obkect which will be inserted.
            VendorProfile vendorProfile = new VendorProfile() { VendorDetails = "Abc Appraisals", VendorProfileId = 1, };
            vendorProfile.Tenant = new TenantModel();
            // objPolicyHelper.SetTenantIdsForInsert(vendorProfile);
        }

        [TestMethod]
        public void TestPolicyHelperReadByServicer()
        {
            // Get the policy.
            var path = Path.Combine("..\\.." + Directory.GetCurrentDirectory(), "\\MyDocumentPolicy.json");
            string policyDoc = File.ReadAllText(@"..\..\..\MyDocumentPolicy.json");

            // IPolicyContext policyCtx = new PolicyContext(new UserContext());

            // policyCtx.SetCustomIntField("MasterClientProfileId", 3423);

            // policyCtx.SetCustomIntField("SubcontractorTenantId", 2);

            // new up Policy helper and Policy context.
            // PolicyHelper objPolicyHelper = new PolicyHelper(PolicyDoc, policyCtx);
            var parameters = new SqlParameterCollection();
            // objPolicyHelper.GetCosmosWhereClause("VP", parameters,null);
        }

        [TestMethod]
        public void TestPolicyHelperReadByVendor()
        {
            // Get the policy.
            var path = Path.Combine("..\\.." + Directory.GetCurrentDirectory(), "\\MyDocumentPolicy.json");
            string policyDoc = File.ReadAllText(@"..\..\..\MyDocumentPolicy.json");

            // IPolicyContext policyCtx = new PolicyContext(new UserContext());

            // policyCtx.SetCustomIntField("MasterClientProfileId", 3423);

            // policyCtx.SetCustomIntField("SubcontractorTenantId", 2);

            // new up Policy helper and Policy context.
            // PolicyHelper objPolicyHelper = new PolicyHelper(PolicyDoc, policyCtx);

            var parameters = new SqlParameterCollection();

            // objPolicyHelper.GetCosmosWhereClause("VP", parameters, null);
        }

        [TestMethod]
        public void TestPolicyHelperReadBySubVendor()
        {
            // Get the policy.
            var path = Path.Combine("..\\.." + Directory.GetCurrentDirectory(), "\\MyDocumentPolicy.json");
            string policyDoc = File.ReadAllText(@"..\..\..\MyDocumentPolicy.json");

            // IPolicyContext policyCtx = new PolicyContext(new UserContext());

            // policyCtx.SetCustomIntField("MasterClientProfileId", 3423);

            // policyCtx.SetCustomIntField("SubcontractorTenantId", 2);

            // new up Policy helper and Policy context.
            // PolicyHelper objPolicyHelper = new PolicyHelper(PolicyDoc, policyCtx);
            var parameters = new SqlParameterCollection();
            // objPolicyHelper.GetCosmosWhereClause("VP", parameters, null);
        }

        [TestMethod]
        public void TestPolicyHelperReadByClient()
        {
            // Get the policy.
            var path = Path.Combine("..\\.." + Directory.GetCurrentDirectory(), "\\MyDocumentPolicy.json");
            string policyDoc = File.ReadAllText(@"..\..\..\MyDocumentPolicy.json");

            // IPolicyContext policyCtx = new PolicyContext(new UserContext());

            // policyCtx.SetCustomIntField("MasterClientProfileId", 3423);

            // policyCtx.SetCustomIntField("SubcontractorTenantId", 2);

            // new up Policy helper and Policy context.
            // PolicyHelper objPolicyHelper = new PolicyHelper(PolicyDoc, policyCtx);
            var parameters = new SqlParameterCollection();
            // objPolicyHelper.GetCosmosWhereClause("VP", parameters, null);
        }
    }
}
#pragma warning restore SA1402 // File may only contain a single type
#pragma warning restore SA1649 // File name should match first type name
