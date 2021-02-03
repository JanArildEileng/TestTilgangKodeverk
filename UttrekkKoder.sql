/****** Script for SelectTopNRows command from SSMS  ******/

/* grunndata  */


SELECT TOP (10000) [KodeId]
      ,[OId]
      ,[Verdi]
      ,[Navn]
  FROM [Kodeverk].[Kode]
  where Oid in(9060,7478,7402,7484,9090,3402,99900,7502,7421,7427,9009501,9003103,9003101)