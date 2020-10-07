<?xml version="1.0" encoding="utf-8"?>
<SqlTemplate>
  <Type>Mysql</Type>
  <Select>SELECT /**distinct**/ {0} FROM {1} /**innerjoin**/ /**leftjoin**/ /**rightjoin**/ /**where**/ /**orderby**/ /**take**/ /**skip**/</Select>
  <Update>UPDATE {0} SET {1} /**where**/</Update>
  <Insert>INSERT INTO {0} ({1}) VALUES ({2});</Insert>
  <Delete>DELETE FROM {0} /**where**/</Delete>  
  <Separator>`, `</Separator>
  <Identity>SELECT LAST_INSERT_ID();</Identity>
  <QuoteChar>`{0}`</QuoteChar>  
  <Take>LIMIT</Take>
  <Skip>OFFSET</Skip>
</SqlTemplate>