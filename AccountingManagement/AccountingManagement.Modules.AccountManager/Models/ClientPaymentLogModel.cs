using System;
using System.Collections.Generic;
using System.Text;
using AccountingManagement.DataAccess.Entities;

namespace AccountingManagement.Modules.AccountManager.Models
{
    public class ClientPaymentLogModel
    {
        public Business Business { get; set; }

        public ClientPayment ClientPayment { get; set; }

        public ClientPaymentLog ClientPaymentLog { get; set; }

        public bool IsSelected { get; set; }

        public ClientPaymentLogModel(ClientPaymentLog clientPaymentLog)
        {
            Business = clientPaymentLog.Business;
            ClientPayment = clientPaymentLog.ClientPayment;
            ClientPaymentLog = clientPaymentLog;
        }
    }
}
