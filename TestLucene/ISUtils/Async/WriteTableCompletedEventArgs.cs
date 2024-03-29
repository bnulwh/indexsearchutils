﻿using System;
using System.Collections.Generic;
using System.Text;

namespace ISUtils.Async
{
    public delegate void WriteTableCompletedEventHandler(object sender, WriteTableCompletedEventArgs e);
    public sealed class WriteTableCompletedEventArgs
    {
        private string  tableName="";
        public string  TableName
        {
            get 
            {
                return tableName; 
            }
        }
        public WriteTableCompletedEventArgs(string tablename)
        {
            tableName = tablename;
        }
    }
}
