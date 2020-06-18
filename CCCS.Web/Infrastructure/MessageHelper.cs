using CCCS.Core.Domain.Notices;
using CCCS.Data;
using System;
using System.Linq;

namespace CCCS.Infrastructure
{
    public static class MessageHelper
    {
        public static void SendMessage(int id, string subject, string text, string sender)
        {
            using (ContractContext db = new ContractContext())
            {
                var project = db.Projects.FirstOrDefault(x => x.Id == id);
                var messageThread = db.MessageThreads.FirstOrDefault(x => x.ProjectId == id);
                int messageThreadId;

                if (messageThread == null)
                {
                    MessageThread newThread = new MessageThread
                    {
                        JOC = project.JOC,
                        Subject = subject,
                        ProjectId = id,
                        DateCreated = DateTime.Now
                    };

                    db.MessageThreads.Add(newThread);
                    db.SaveChanges();

                    messageThreadId = newThread.Id;
                }
                else
                {
                    messageThreadId = messageThread.Id;
                }

                var contractor = db.Contractors.Find(project.PrimeContractorID);
                Message message = new Message
                {
                    ThreadId = messageThreadId,
                    Sender = sender,
                    Recipient = (contractor != null && !String.IsNullOrEmpty(contractor.AlternateDCO))? contractor.AlternateDCO: project.DCO,
                    Text = text,
                    DateSent = DateTime.Now
                };

                db.Messages.Add(message);
                db.SaveChanges();
            }
        }
    }
}
