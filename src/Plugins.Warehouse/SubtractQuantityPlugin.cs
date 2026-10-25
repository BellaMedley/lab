using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using TALXIS.DevKit.Build.Dataverse.Plugin;

namespace Plugins.Warehouse
{
    [PluginStep("Create", "lab_warehousetransaction", PluginStage.PostOperation)]
    public class SubtractQuantityPlugin : PluginBase
    {
        public SubtractQuantityPlugin(string unsecureConfiguration, string secureConfiguration)
            : base(typeof(SubtractQuantityPlugin))
        {
        }

        protected override void ExecuteDataversePlugin(ILocalPluginContext localPluginContext)
        {
            if (localPluginContext == null)
            {
                throw new ArgumentNullException(nameof(localPluginContext));
            }

            var context = localPluginContext.PluginExecutionContext;
            if (!context.InputParameters.Contains("Target")
                || !(context.InputParameters["Target"] is Entity target)
                || target.LogicalName != "lab_warehousetransaction")
            {
                return;
            }

            if (!target.Contains("lab_quantity")
                || !target.Contains("lab_itemid")
                || !target.Contains("lab_transactiontype"))
            {
                return;
            }

            try
            {
                var quantity = (int)target["lab_quantity"];
                var itemReference = (EntityReference)target["lab_itemid"];
                var transactionType = (OptionSetValue)target["lab_transactiontype"];
                var service = localPluginContext.OrgSvcFactory.CreateOrganizationService(context.UserId);
                var item = service.Retrieve(
                    "lab_warehouseitem",
                    itemReference.Id,
                    new ColumnSet("lab_availablequantity"));

                var available = item != null && item.Contains("lab_availablequantity")
                    ? (int)item["lab_availablequantity"]
                    : 0;

                var itemUpdate = new Entity("lab_warehouseitem", itemReference.Id);
                if (transactionType.Value == 100000000)
                {
                    itemUpdate["lab_availablequantity"] = available + quantity;
                }
                else if (transactionType.Value == 100000001)
                {
                    itemUpdate["lab_availablequantity"] = available - quantity;
                }
                else
                {
                    return;
                }

                service.Update(itemUpdate);
            }
            catch (Exception exception)
            {
                localPluginContext.TracingService.Trace(
                    "SubtractQuantityPlugin failed: {0}",
                    exception);
                throw;
            }
        }
    }
}
