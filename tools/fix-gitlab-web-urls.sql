DECLARE @source uniqueidentifier='6824062D-9C19-49B9-857E-44E84BAFE77E';
UPDATE dbo.SourceSnapshots SET Payload=REPLACE(Payload,'https://192.168.1.155/','https://5.160.84.190:60443/') WHERE ConnectionId=@source AND (Scope LIKE 'requests:%' OR Scope='repositories');
UPDATE dbo.ReviewRuns SET MergeRequest=JSON_MODIFY(MergeRequest,'$.WebUrl',REPLACE(JSON_VALUE(MergeRequest,'$.WebUrl'),'https://192.168.1.155/','https://5.160.84.190:60443/')) WHERE JSON_VALUE(MergeRequest,'$.ConnectionId')=CONVERT(varchar(36),@source) AND JSON_VALUE(MergeRequest,'$.WebUrl') LIKE 'https://192.168.1.155/%';
