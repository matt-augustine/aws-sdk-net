﻿using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using AWSSDK_DotNet.IntegrationTests.Utils;

using Amazon.AWSSupport;
using Amazon.AWSSupport.Model;
using Amazon;
using System.IO;
using System.Text;

namespace AWSSDK_DotNet.IntegrationTests.Tests
{
    [TestClass]
    public class AWSSupportTests : TestBase<AmazonAWSSupportClient>
    {
        private static String
            SUBJECT = ".NET SDK Test Case " + DateTime.UtcNow.Ticks,
            CATEGORY_CODE = "apis",
            SERVICE_CODE = "amazon-dynamodb",
            COMMUNICATION_BODY = "This is a test case generated by the .NET SDK integration test suite",
            LANGUAGE = "ja",
            SEVERITY_CODE = "low",
            ATTACHMENT_CONTENTS = "This is test data";

        [ClassCleanup]
        public static void ClassCleanup()
        {
            BaseClean();
        }

        //  Test are disabled because not all acounts are subscribed to AWS Support
        //        [TestMethod]
        public void TestCaseOperations()
        {
            string caseId = null;

            try
            {
                caseId = Client.CreateCase(new CreateCaseRequest
                {
                    Subject = SUBJECT,
                    CategoryCode = CATEGORY_CODE,
                    ServiceCode = SERVICE_CODE,
                    Language = LANGUAGE,
                    SeverityCode = SEVERITY_CODE,
                    CommunicationBody = COMMUNICATION_BODY
                }).CaseId;

                Assert.IsNotNull(caseId);

                var cases = Client.DescribeCases(new DescribeCasesRequest { Language = LANGUAGE }).Cases;
                Assert.IsTrue(cases.Count > 0);

                cases = Client.DescribeCases(new DescribeCasesRequest { Language = LANGUAGE, CaseIdList = new List<string> { caseId } }).Cases;
                Assert.AreEqual(1, cases.Count);

                Assert.AreEqual(caseId, cases[0].CaseId);
                Assert.AreEqual(CATEGORY_CODE, cases[0].CategoryCode);
                Assert.AreEqual(LANGUAGE, cases[0].Language);
                Assert.AreEqual(SERVICE_CODE, cases[0].ServiceCode);
                Assert.AreEqual(SEVERITY_CODE, cases[0].SeverityCode);
                Assert.IsTrue(cases[0].RecentCommunications.Communications.Count > 0);

                var attachmentData = new MemoryStream(Encoding.UTF8.GetBytes(ATTACHMENT_CONTENTS));
                var filename = "file1.txt";
                var attachmentSetId = Client.AddAttachmentsToSet(new AddAttachmentsToSetRequest
                {
                    Attachments = new List<Attachment>
                    {
                        new Attachment
                        {
                            FileName = filename,
                            Data = attachmentData
                        }
                    }
                }).AttachmentSetId;

                var result = Client.AddCommunicationToCase(new AddCommunicationToCaseRequest
                {
                    CaseId = caseId,
                    CcEmailAddresses = new List<string> { "aws-dr-tools-test@amazon.com" },
                    CommunicationBody = COMMUNICATION_BODY,
                    AttachmentSetId = attachmentSetId
                });

                Assert.IsNotNull(result);

                var comms = Client.DescribeCommunications(new DescribeCommunicationsRequest { CaseId = caseId }).Communications;
                Assert.IsTrue(comms.Count > 0);
                Assert.AreEqual(caseId, comms[0].CaseId);
                Assert.AreEqual(COMMUNICATION_BODY.Trim(), comms[0].Body.Trim());
                Assert.IsNotNull(comms[0].SubmittedBy);
                Assert.IsNotNull(comms[0].TimeCreated);

                string attachmentId = null;
                attachmentId = GetAttachmentId(comms, attachmentId);
                Assert.IsNotNull(attachmentId);

                VerifyAttachment(attachmentData, filename, attachmentId);

                cases = Client.DescribeCases(new DescribeCasesRequest { Language = LANGUAGE, CaseIdList = new List<string> { caseId }, IncludeCommunications = true }).Cases;
                Assert.AreEqual(1, cases.Count);
                var communications = cases[0].RecentCommunications;
                attachmentId = GetAttachmentId(communications.Communications, attachmentId);
                VerifyAttachment(attachmentData, filename, attachmentId);
            }
            finally
            {
                if (caseId != null)
                {
                    Client.ResolveCase(new ResolveCaseRequest { CaseId = caseId });
                }
            }
        }

        private static void VerifyAttachment(MemoryStream attachmentData, string filename, string attachmentId)
        {
            var attachment = Client.DescribeAttachment(new DescribeAttachmentRequest
            {
                AttachmentId = attachmentId
            }).Attachment;
            Assert.IsNotNull(attachment);
            Assert.AreEqual(
                Encoding.UTF8.GetString(attachmentData.ToArray()),
                Encoding.UTF8.GetString(attachment.Data.ToArray()));
            Assert.AreEqual(filename, attachment.FileName);
        }

        private static string GetAttachmentId(List<Communication> comms, string attachmentId)
        {
            foreach (var comm in comms)
            {
                var attachmentSet = comm.AttachmentSet;
                if (attachmentSet != null && attachmentSet.Count > 0)
                {
                    foreach (var att in attachmentSet)
                    {
                        if (!string.IsNullOrEmpty(att.AttachmentId))
                            attachmentId = att.AttachmentId;
                    }
                }
            }
            return attachmentId;
        }

        //  Test are disabled because not all acounts are subscribed to AWS Support
        //        [TestMethod]
        public void TestDescribeServices()
        {
            var services = Client.DescribeServices().Services;
            Assert.IsTrue(services.Count > 0);
            Assert.IsNotNull(services[0].Code);
            Assert.IsNotNull(services[0].Name);
            Assert.IsTrue(services[0].Categories.Count > 0);
            Assert.IsNotNull(services[0].Categories[0].Code);
            Assert.IsNotNull(services[0].Categories[0].Name);

            services = Client.DescribeServices(new DescribeServicesRequest { ServiceCodeList = new List<string> { SERVICE_CODE } }).Services;
            Assert.AreEqual(1, services.Count);
            Assert.IsNotNull(services[0].Name);
            Assert.AreEqual(SERVICE_CODE, services[0].Code);
        }

        //  Test are disabled because not all acounts are subscribed to AWS Support
        //       [TestMethod]
        public void TestSeverityLevels()
        {
            var levels = Client.DescribeSeverityLevels().SeverityLevels;
            Assert.IsTrue(levels.Count > 0);
            Assert.IsNotNull(levels[0].Name);
            Assert.IsNotNull(levels[0].Code);
        }

        //  Test are disabled because not all acounts are subscribed to AWS Support
        //       [TestMethod]
        public void TestTrustedAdvisorChecks()
        {
            var checks = Client.DescribeTrustedAdvisorChecks(new DescribeTrustedAdvisorChecksRequest { Language = LANGUAGE }).Checks;
            Assert.IsTrue(checks.Count > 0);

            var checkId = checks[0].Id;
            Assert.IsNotNull(checks[0].Name);
            Assert.IsNotNull(checks[0].Category);
            Assert.IsNotNull(checks[0].Description);
            Assert.IsTrue(checks[0].Metadata.Count > 0);
            Assert.IsNotNull(checks[0].Metadata[0]);

            var statuses = Client.DescribeTrustedAdvisorCheckRefreshStatuses(new DescribeTrustedAdvisorCheckRefreshStatusesRequest { CheckIds = new List<string> { checkId } })
                .Statuses;

            Assert.AreEqual(1, statuses.Count);
            Assert.AreEqual(checkId, statuses[0].CheckId);
            Assert.IsNotNull(statuses[0].Status);
            Assert.IsNotNull(statuses[0].MillisUntilNextRefreshable);

            var status = Client.RefreshTrustedAdvisorCheck(new RefreshTrustedAdvisorCheckRequest { CheckId = checkId }).Status;
            Assert.IsNotNull(status);

            var summaries = Client.DescribeTrustedAdvisorCheckSummaries(new DescribeTrustedAdvisorCheckSummariesRequest { CheckIds = new List<string> { checkId } })
                .Summaries;

            Assert.AreEqual(1, summaries.Count);
            Assert.AreEqual(checkId, summaries[0].CheckId);
            Assert.IsNotNull(summaries[0].Status);
            Assert.IsNotNull(summaries[0].Timestamp);
            Assert.IsNotNull(summaries[0].ResourcesSummary);
            Assert.IsNotNull(summaries[0].CategorySpecificSummary);

            var resultresult = Client.DescribeTrustedAdvisorCheckResult(new DescribeTrustedAdvisorCheckResultRequest { CheckId = checkId })
                .Result;

            Assert.IsNotNull(resultresult.Timestamp);
            Assert.IsNotNull(resultresult.Status);
            Assert.IsNotNull(resultresult.ResourcesSummary);
        }
    }
}
