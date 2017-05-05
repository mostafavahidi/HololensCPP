//*********************************************************
//
// Copyright (c) Microsoft. All rights reserved.
// This code is licensed under the MIT License (MIT).
// THIS CODE IS PROVIDED *AS IS* WITHOUT WARRANTY OF
// ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING ANY
// IMPLIED WARRANTIES OF FITNESS FOR A PARTICULAR
// PURPOSE, MERCHANTABILITY, OR NON-INFRINGEMENT.
//
//*********************************************************

using System;
using System.Collections.Generic;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using Windows.Devices.PointOfService;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using SDKTemplate;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace PosPrinterSample
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class Scenario1_ReceiptPrinter : Page
    {
        private MainPage rootPage;

        PosPrinter printer = null;
        ClaimedPosPrinter claimedPrinter = null;
        bool IsAnImportantTransaction = true;

        public Scenario1_ReceiptPrinter()
        {
            this.InitializeComponent();
        }

        private void ResetTheScenarioState()
        {
            //Remove releasedevicerequested handler and dispose claimed printer object.
            if (claimedPrinter != null)
            {
                claimedPrinter.ReleaseDeviceRequested -= ClaimedPrinter_ReleaseDeviceRequested;
                claimedPrinter.Dispose();
                claimedPrinter = null;
            }

            if (printer != null)
            {
                printer.Dispose();
                printer = null;
            }
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            rootPage = MainPage.Current;
            ResetTheScenarioState();
        }

        /// <summary>
        /// Invoked immediately before the Page is unloaded and is no longer the current source of a parent Frame
        /// </summary>
        /// <param name="e">Provides data for the OnNavigatingFrom callback that can be used to cancel a navigation 
        /// request from origination</param>
        protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            ResetTheScenarioState();
        }

        /// <summary>
        /// Creates multiple tasks, first to find a receipt printer, then to claim the printer and then to enable and add releasedevicerequested event handler.
        /// </summary>
        async void FindClaimEnable_Click(object sender, RoutedEventArgs e)
        {
            if (await FindReceiptPrinter())
            {
                if (await ClaimPrinter())
                {
                    await EnableAsync();
                }
            }
        }

        /// <summary>
        /// Default checkbox selection makes it an important transaction, hence we do not release claim when we get a release devicerequested event.
        /// </summary>
        async void ClaimedPrinter_ReleaseDeviceRequested(ClaimedPosPrinter sender, PosPrinterReleaseDeviceRequestedEventArgs args)
        {
            if (IsAnImportantTransaction)
            {
                await sender.RetainDeviceAsync();
            }
            else
            {
                ResetTheScenarioState();
            }
        }

        /// <summary>
        /// Prints the line that is in the textbox.
        /// </summary>
        async void PrintLine_Click(object sender, RoutedEventArgs e)
        {
            await PrintLine();
        }

        /// <summary>
        /// Prints a sample receipt
        /// </summary>
        async void PrintReceipt_Click(object sender, RoutedEventArgs e)
        {
            rootPage.NotifyUser("Trying to Print Receipt", NotifyType.StatusMessage);
            Dictionary<string, double> receiptItems = new Dictionary<string, double>();
            receiptItems.Add("Books", 10.40);
            receiptItems.Add("Games", 9.60);
            await PrintReceipt(receiptItems);
        }

        private void EndScenario_Click(object sender, RoutedEventArgs e)
        {
            ResetTheScenarioState();
            rootPage.NotifyUser("Disconnected Printer", NotifyType.StatusMessage);
        }

        /// <summary>
        /// Actual claim method task that claims the printer asynchronously
        /// </summary>
        private async Task<bool> ClaimPrinter()
        {
            if (claimedPrinter == null)
            {
                claimedPrinter = await printer.ClaimPrinterAsync();
                if (claimedPrinter != null)
                {
                    claimedPrinter.ReleaseDeviceRequested += ClaimedPrinter_ReleaseDeviceRequested;
                    rootPage.NotifyUser("Claimed Printer", NotifyType.StatusMessage);
                }
                else
                {
                    rootPage.NotifyUser("Claim Printer failed", NotifyType.ErrorMessage);
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Enable the claimed printer.
        /// </summary>
        private async Task<bool> EnableAsync()
        {
            if (claimedPrinter == null)
            {
                rootPage.NotifyUser("No Claimed Printer to enable", NotifyType.ErrorMessage);
                return false;
            }

            if (!await claimedPrinter.EnableAsync())
            {
                rootPage.NotifyUser("Could not enable printer", NotifyType.ErrorMessage);
                return false;
            }

            rootPage.NotifyUser("Enabled printer.", NotifyType.StatusMessage);

            return true;
        }

        private async Task<bool> PrintLine()
        {
            if (!IsPrinterClaimed())
            {
                return false;
            }
            ReceiptPrintJob job = claimedPrinter.Receipt.CreateJob();
            job.PrintLine(txtPrintLine.Text);

            if (await job.ExecuteAsync())
            {
                rootPage.NotifyUser("Printed line", NotifyType.StatusMessage);
                return true;
            }
            rootPage.NotifyUser("Was not able to print line", NotifyType.StatusMessage);
            return false;
        }

        /// <summary>
        /// Check to make sure we still have claim on printer
        /// </summary>
        private bool IsPrinterClaimed()
        {
            if (printer == null)
            {
                rootPage.NotifyUser("Need to find printer first", NotifyType.ErrorMessage);
                return false;
            }

            if (claimedPrinter == null)
            {
                rootPage.NotifyUser("No claimed printer.", NotifyType.ErrorMessage);
                return false;
            }

            if (claimedPrinter.Receipt == null)
            {
                rootPage.NotifyUser("No receipt printer object in claimed printer.", NotifyType.ErrorMessage);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Prints a sample receipt.
        /// </summary>
        private async Task<bool> PrintReceipt(Dictionary<string, double> receiptItems)
        {
            if (!IsPrinterClaimed())
            {
                return false;
            }

            string receiptString = "======================\n" +
                                   "|   Sample Header    |\n" +
                                   "======================\n" +
                                   "Item             Price\n" +
                                   "----------------------\n";

            double total = 0.0;
            foreach (KeyValuePair<string, double> item in receiptItems)
            {
                receiptString += string.Format("{0,-15} {1,6:##0.00}\n", item.Key, item.Value);
                total += item.Value;
            }
            receiptString += "----------------------\n";
            receiptString += string.Format("Total-----------{0,6:##0.00}\n", total);

            ReceiptPrintJob merchantJob = claimedPrinter.Receipt.CreateJob();
            string merchantFooter = GetMerchantFooter();
            PrintLineFeedAndCutPaper(merchantJob, receiptString + merchantFooter);

            ReceiptPrintJob customerJob = claimedPrinter.Receipt.CreateJob();
            string customerFooter = GetCustomerFooter();
            PrintLineFeedAndCutPaper(customerJob, receiptString + customerFooter);

            // execute print jobs 
            // Note that we actually execute "job" twice in order to print 
            // the statement for both the customer as well as the merchant. 
            if (!(await merchantJob.ExecuteAsync()) ||
                !(await customerJob.ExecuteAsync()))
            {
                rootPage.NotifyUser("Failed to print receipts.", NotifyType.ErrorMessage);
                return false;
            }
            rootPage.NotifyUser("Printed receipts.", NotifyType.StatusMessage);
            return true;
        }

        private string GetMerchantFooter()
        {
            return "\n" +
                   "______________________\n" +
                   "Tip\n" +
                   "\n" +
                   "______________________\n" +
                   "Signature\n" +
                   "\n" +
                   "Merchant Copy\n";
        }

        private string GetCustomerFooter()
        {
            return "\n" +
                   "______________________\n" +
                   "Tip\n" +
                   "\n" +
                   "Customer Copy\n";
        }

        // Cut the paper after printing enough blank lines to clear the paper cutter.
        private void PrintLineFeedAndCutPaper(ReceiptPrintJob job, string receipt)
        {
            if (IsPrinterClaimed())
            {
                // Passing a multi-line string to the Print method results in
                // smoother paper feeding than sending multiple single-line strings
                // to PrintLine.

                string feedString = "";
                for (uint n = 0; n < claimedPrinter.Receipt.LinesToPaperCut; n++)
                {
                    feedString += "\n";
                }
                job.Print(receipt + feedString);
                if (printer.Capabilities.Receipt.CanCutPaper)
                {
                    job.CutPaper();
                }
            }
        }

        private async Task<bool> FindReceiptPrinter()
        {
            if (printer == null)
            {
                rootPage.NotifyUser("Finding printer", NotifyType.StatusMessage);
                printer = await DeviceHelpers.GetFirstReceiptPrinterAsync();
                if (printer != null)
                {
                    rootPage.NotifyUser("Got Printer with Device Id : " + printer.DeviceId, NotifyType.StatusMessage);
                    return true;
                }
                else
                {
                    rootPage.NotifyUser("No Printer found", NotifyType.ErrorMessage);
                    return false;
                }
            }
            return true;
        }
    }
}
