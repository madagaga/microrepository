﻿<?xml version="1.0" encoding="utf-8"?>
<SqlTemplate>
  <Type>MSSql</Type>
  <Select>SELECT /**distinct**/ /**take**/ {0} FROM {1} /**innerjoin**/ /**leftjoin**/ /**rightjoin**/ /**where**/ /**orderby**/ /**skip**/  </Select>
  <Update>UPDATE {0} SET {1} /**where**/</Update>
  <Insert>INSERT INTO {0} ({1}) VALUES ({2});</Insert>
  <Delete>DELETE FROM {0} /**where**/</Delete>  
  <Identity>SELECT SCOPE_IDENTITY();</Identity>
  <QuoteChar>[{0}]</QuoteChar>
</SqlTemplate>