using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Tridion;
using Tridion.ContentManager.CoreService.Client;

namespace ReleaseManager
{
    public static class Utilities
    {
        public static string getFancyDateTime(DateTime? dt)
        {
            return DateTime.SpecifyKind(dt.HasValue ? dt.Value : DateTime.Now, DateTimeKind.Utc).ToString("o");
        }

        public static string getCurrentUser()
        {
            return HttpContext.Current.User.Identity.Name.Replace("CORP\\", "");
        }

        public static ReleaseItem getProspectiveReleaseItemByWebdav(string uri, SessionAwareCoreServiceClient tridionClient)
        {
            var item = tridionClient.Read(uri, new ReadOptions());
            if (item == null)
            {
                return null;
            }
            else
            {
                return getProspectiveReleaseItem(item.Id, tridionClient);
            }
        }

        /// <summary>
        /// Retrieves information from Tridion to create a releaseManagerObject
        /// </summary>
        /// <param name="uri"></param>
        /// <returns></returns>
        public static ReleaseItem getProspectiveReleaseItem(string uri, SessionAwareCoreServiceClient tridionClient)
        {
            //Define itemtype            
            int itemtype = ReleaseManagerRepository.getItemTypeByURI(uri);

            Tridion.ContentManager.ItemType itemType = (Tridion.ContentManager.ItemType)itemtype;
            ReleaseItem returnItem = new ReleaseItem();

            // TODO: TBD: why is this using a switch?
            switch (itemType)
            {
                case Tridion.ContentManager.ItemType.ActivityDefinition:
                    break;
                case Tridion.ContentManager.ItemType.ActivityHistory:
                    break;
                case Tridion.ContentManager.ItemType.ActivityInstance:
                    break;
                case Tridion.ContentManager.ItemType.ApprovalStatus:
                    break;
                case Tridion.ContentManager.ItemType.Category:
                    CategoryData categoryData = (CategoryData)tridionClient.Read(uri, new ReadOptions());
                    returnItem.TITLE = categoryData.Title;
                    returnItem.URI = categoryData.Id;
                    returnItem.WEBDAV_URL = categoryData.LocationInfo.WebDavUrl;
                    returnItem.ISSHARED = (bool)categoryData.BluePrintInfo.IsShared;
                    returnItem.SUBTYPE = 0;
                    break;
                case Tridion.ContentManager.ItemType.Component:
                    //IdentifiableObjectData data = (IdentifiableObjectData)tridionClient.Read(uri, new ReadOptions());
                    ComponentData componentData = (ComponentData)tridionClient.Read(uri, new ReadOptions());
                    if (componentData.BluePrintInfo.IsLocalized != true)
                    {
                        uri = tridionClient.GetTcmUri(componentData.Id, componentData.BluePrintInfo.OwningRepository.IdRef, null);
                        componentData = (ComponentData)tridionClient.Read(uri, new ReadOptions());
                    }

                    returnItem.TITLE = componentData.Title;
                    returnItem.URI = componentData.Id;
                    returnItem.WEBDAV_URL = componentData.LocationInfo.WebDavUrl;
                    returnItem.ISSHARED = (bool)componentData.BluePrintInfo.IsShared;
                    returnItem.SUBTYPE = (componentData.ComponentType == ComponentType.Normal) ? 0 : 1;
                    //returnItem.SUBTYPE = int.Parse(componentData.ComponentType.Value.ToString());

                    break;
                case Tridion.ContentManager.ItemType.ComponentTemplate:
                    ComponentTemplateData componentTemplateData = (ComponentTemplateData)tridionClient.Read(uri, new ReadOptions());
                    if (componentTemplateData.BluePrintInfo.IsLocalized != true)
                    {
                        uri = tridionClient.GetTcmUri(componentTemplateData.Id, componentTemplateData.BluePrintInfo.OwningRepository.IdRef, null);
                        componentTemplateData = (ComponentTemplateData)tridionClient.Read(uri, new ReadOptions());
                    }
                    returnItem.TITLE = componentTemplateData.Title;
                    returnItem.URI = componentTemplateData.Id;
                    returnItem.WEBDAV_URL = componentTemplateData.LocationInfo.WebDavUrl;
                    returnItem.ISSHARED = (bool)componentTemplateData.BluePrintInfo.IsShared;
                    returnItem.SUBTYPE = 0;
                    break;
                case Tridion.ContentManager.ItemType.DirectoryGroupMapping:
                    break;
                case Tridion.ContentManager.ItemType.DirectoryService:
                    break;
                case Tridion.ContentManager.ItemType.Folder:
                    FolderData folderData = (FolderData)tridionClient.Read(uri, new ReadOptions());
                    if (folderData.BluePrintInfo.IsLocalized != true)
                    {
                        uri = tridionClient.GetTcmUri(folderData.Id, folderData.BluePrintInfo.OwningRepository.IdRef, null);
                        folderData = (FolderData)tridionClient.Read(uri, new ReadOptions());
                    }
                    returnItem.TITLE = folderData.Title;
                    returnItem.URI = folderData.Id;
                    returnItem.WEBDAV_URL = folderData.LocationInfo.WebDavUrl;
                    returnItem.ISSHARED = (bool)folderData.BluePrintInfo.IsShared;
                    returnItem.SUBTYPE = 0;
                    break;
                case Tridion.ContentManager.ItemType.Group:
                    break;
                case Tridion.ContentManager.ItemType.Keyword:
                    KeywordData keywordData = (KeywordData)tridionClient.Read(uri, new ReadOptions());
                    if (keywordData.BluePrintInfo.IsLocalized != true)
                    {
                        uri = tridionClient.GetTcmUri(keywordData.Id, keywordData.BluePrintInfo.OwningRepository.IdRef, null);
                        keywordData = (KeywordData)tridionClient.Read(uri, new ReadOptions());
                    }
                    returnItem.TITLE = keywordData.Title;
                    returnItem.URI = keywordData.Id;
                    returnItem.WEBDAV_URL = keywordData.LocationInfo.WebDavUrl;
                    returnItem.ISSHARED = (bool)keywordData.BluePrintInfo.IsShared;
                    returnItem.SUBTYPE = 0;
                    break;
                case Tridion.ContentManager.ItemType.MultimediaType:
                    break;
                //case Tridion.ContentManager.ItemType.MultipleOperations:
                //    break;
                case Tridion.ContentManager.ItemType.None:
                    break;
                case Tridion.ContentManager.ItemType.Page:
                    PageData pageData = (PageData)tridionClient.Read(uri, new ReadOptions());
                    if (pageData.BluePrintInfo.IsLocalized != true)
                    {
                        uri = tridionClient.GetTcmUri(pageData.Id, pageData.BluePrintInfo.OwningRepository.IdRef, null);
                        pageData = (PageData)tridionClient.Read(uri, new ReadOptions());
                    }
                    returnItem.TITLE = pageData.Title;
                    returnItem.URI = pageData.Id;
                    returnItem.WEBDAV_URL = pageData.LocationInfo.WebDavUrl;
                    returnItem.ISSHARED = (bool)pageData.BluePrintInfo.IsShared;
                    returnItem.SUBTYPE = 0;
                    break;
                case Tridion.ContentManager.ItemType.PageTemplate:
                    PageTemplateData pageTemplateData = (PageTemplateData)tridionClient.Read(uri, new ReadOptions());
                    if (pageTemplateData.BluePrintInfo.IsLocalized != true)
                    {
                        uri = tridionClient.GetTcmUri(pageTemplateData.Id, pageTemplateData.BluePrintInfo.OwningRepository.IdRef, null);
                        pageTemplateData = (PageTemplateData)tridionClient.Read(uri, new ReadOptions());
                    }
                    returnItem.TITLE = pageTemplateData.Title;
                    returnItem.URI = pageTemplateData.Id;
                    returnItem.WEBDAV_URL = pageTemplateData.LocationInfo.WebDavUrl;
                    returnItem.ISSHARED = (bool)pageTemplateData.BluePrintInfo.IsShared;
                    returnItem.SUBTYPE = 0;
                    break;
                case Tridion.ContentManager.ItemType.ProcessDefinition:
                    break;
                case Tridion.ContentManager.ItemType.ProcessHistory:
                    break;
                case Tridion.ContentManager.ItemType.ProcessInstance:
                    break;
                case Tridion.ContentManager.ItemType.Publication:
                    PublicationData publicationData = (PublicationData)tridionClient.Read(uri, new ReadOptions());
                    returnItem.TITLE = publicationData.Title;
                    returnItem.URI = publicationData.Id;
                    returnItem.WEBDAV_URL = publicationData.LocationInfo.WebDavUrl;
                    returnItem.ISSHARED = false;
                    returnItem.SUBTYPE = 0;
                    break;
                case Tridion.ContentManager.ItemType.PublicationTarget:
                    break;
                case Tridion.ContentManager.ItemType.PublishTransaction:
                    break;
                case Tridion.ContentManager.ItemType.Schema:
                    SchemaData schemaData = (SchemaData)tridionClient.Read(uri, new ReadOptions());
                    if (schemaData.BluePrintInfo.IsLocalized != true)
                    {
                        uri = tridionClient.GetTcmUri(schemaData.Id, schemaData.BluePrintInfo.OwningRepository.IdRef, null);
                        schemaData = (SchemaData)tridionClient.Read(uri, new ReadOptions());
                    }
                    returnItem.TITLE = schemaData.Title;
                    returnItem.URI = schemaData.Id;
                    returnItem.WEBDAV_URL = schemaData.LocationInfo.WebDavUrl;
                    returnItem.ISSHARED = (bool)schemaData.BluePrintInfo.IsShared;
                    returnItem.SUBTYPE = 0;
                    break;
                case Tridion.ContentManager.ItemType.StructureGroup:
                    StructureGroupData structureGroupData = (StructureGroupData)tridionClient.Read(uri, new ReadOptions());
                    if (structureGroupData.BluePrintInfo.IsLocalized != true)
                    {
                        uri = tridionClient.GetTcmUri(structureGroupData.Id, structureGroupData.BluePrintInfo.OwningRepository.IdRef, null);
                        structureGroupData = (StructureGroupData)tridionClient.Read(uri, new ReadOptions());
                    }
                    returnItem.TITLE = structureGroupData.Title;
                    returnItem.URI = structureGroupData.Id;
                    returnItem.WEBDAV_URL = structureGroupData.LocationInfo.WebDavUrl;
                    returnItem.ISSHARED = (bool)structureGroupData.BluePrintInfo.IsShared;
                    returnItem.SUBTYPE = 0;
                    break;
                case Tridion.ContentManager.ItemType.TargetDestination:
                    break;
                case Tridion.ContentManager.ItemType.TargetGroup:
                    break;
                case Tridion.ContentManager.ItemType.TargetType:
                    break;
                case Tridion.ContentManager.ItemType.TemplateBuildingBlock:
                    TemplateBuildingBlockData templateBuildingBlockData = (TemplateBuildingBlockData)tridionClient.Read(uri, new ReadOptions());
                    if (templateBuildingBlockData.BluePrintInfo.IsLocalized != true)
                    {
                        uri = tridionClient.GetTcmUri(templateBuildingBlockData.Id, templateBuildingBlockData.BluePrintInfo.OwningRepository.IdRef, null);
                        templateBuildingBlockData = (TemplateBuildingBlockData)tridionClient.Read(uri, new ReadOptions());
                    }
                    returnItem.TITLE = templateBuildingBlockData.Title;
                    returnItem.URI = templateBuildingBlockData.Id;
                    returnItem.WEBDAV_URL = templateBuildingBlockData.LocationInfo.WebDavUrl;
                    returnItem.ISSHARED = (bool)templateBuildingBlockData.BluePrintInfo.IsShared;
                    returnItem.SUBTYPE = 0;
                    break;
                case Tridion.ContentManager.ItemType.User:
                    break;
                case Tridion.ContentManager.ItemType.VirtualFolder:
                    break;
                case Tridion.ContentManager.ItemType.WorkItem:
                    break;
                default:
                    break;
            }
            return returnItem;
        }

    }
}