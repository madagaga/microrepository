﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace MicroRepository.Core.Schema
{
    public class DbRow : Dictionary<string, object>
    {

        internal DbRow(List<string> columns, object[] rowData)
        {
            for (int i = 0; i < columns.Count; i++)
                this.Add(columns[i], rowData[i]);
        }
    }
}
