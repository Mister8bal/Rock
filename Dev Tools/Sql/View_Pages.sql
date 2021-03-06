SELECT 
  p.[InternalName] as [Page.InternalName],
  p.[PageTitle] as [Page.PageTitle],
  p.[Guid] as [Page.Guid],
  l.[Name] as [Page.Layout.Name],
  parentPage.[InternalName] as [ParentPage.InternalName],
  parentPage.[Guid] as [ParentPage.Guid],
  p.[Order]
FROM 
  [Page] p
join [Page] parentPage on p.ParentPageId = parentPage.Id
join [Layout] [l] on p.LayoutId = l.Id
order by [p].[InternalName]


