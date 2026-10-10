DECLARE @id uniqueidentifier=(SELECT TOP 1 Id FROM dbo.Connections WHERE Provider='GitLab' AND Endpoint='https://5.160.84.190:60443');
IF @id IS NOT NULL AND NOT EXISTS(SELECT 1 FROM dbo.SourceSnapshots WHERE ConnectionId=@id AND Scope='requests:23')
BEGIN
DECLARE @payload nvarchar(max)=(SELECT @id AS ConnectionId,'23' AS RepositoryId,4 AS Number,'01 cwe 20 input validation' AS Title,'' AS Sha,'https://5.160.84.190:60443/dmp_portal/dmp/-/merge_requests/4' AS WebUrl,'Mohammad Lotfi' AS Author FOR JSON PATH);
INSERT dbo.SourceSnapshots VALUES(@id,'requests:23',@payload,SYSUTCDATETIME());
END;
