using Opc.Ua;
using Opc.Ua.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Odenwald.OpcUaPlugin
{
    public static class OpcUaHelper
    {
        /// <summary>
        /// Returns the node ids for a set of relative paths.
        /// </summary>
        /// <param name="session">An open session with the server to use.</param>
        /// <param name="startNodeId">The starting node for the relative paths.</param>
        /// <param name="namespacesUris">The namespace URIs referenced by the relative paths.</param>
        /// <param name="relativePaths">The relative paths.</param>
        /// <returns>A collection of local nodes.</returns>
        public static List<NodeId> TranslateBrowsePaths(
            Session session,
            NodeId startNodeId,
            NamespaceTable namespacesUris,
            params string[] relativePaths)
        {
            // build the list of browse paths to follow by parsing the relative paths.
            BrowsePathCollection browsePaths = new BrowsePathCollection();

            if (relativePaths != null)
            {
                for (int ii = 0; ii < relativePaths.Length; ii++)
                {
                    BrowsePath browsePath = new BrowsePath();

                    // The relative paths used indexes in the namespacesUris table. These must be 
                    // converted to indexes used by the server. An error occurs if the relative path
                    // refers to a namespaceUri that the server does not recognize.

                    // The relative paths may refer to ReferenceType by their BrowseName. The TypeTree object
                    // allows the parser to look up the server's NodeId for the ReferenceType.

                    browsePath.RelativePath = RelativePath.Parse(
                        relativePaths[ii],
                        session.TypeTree,
                        namespacesUris,
                        session.NamespaceUris);

                    browsePath.StartingNode = startNodeId;

                    browsePaths.Add(browsePath);
                }
            }

            // make the call to the server.
            BrowsePathResultCollection results;
            DiagnosticInfoCollection diagnosticInfos;

            ResponseHeader responseHeader = session.TranslateBrowsePathsToNodeIds(
                null,
                browsePaths,
                out results,
                out diagnosticInfos);

            // ensure that the server returned valid results.
            Session.ValidateResponse(results, browsePaths);
            Session.ValidateDiagnosticInfos(diagnosticInfos, browsePaths);

            // collect the list of node ids found.
            List<NodeId> nodes = new List<NodeId>();

            for (int ii = 0; ii < results.Count; ii++)
            {
                // check if the start node actually exists.
                if (StatusCode.IsBad(results[ii].StatusCode))
                {
                    nodes.Add(null);
                    continue;
                }

                // an empty list is returned if no node was found.
                if (results[ii].Targets.Count == 0)
                {
                    nodes.Add(null);
                    continue;
                }

                // Multiple matches are possible, however, the node that matches the type model is the
                // one we are interested in here. The rest can be ignored.
                BrowsePathTarget target = results[ii].Targets[0];

                if (target.RemainingPathIndex != UInt32.MaxValue)
                {
                    nodes.Add(null);
                    continue;
                }

                // The targetId is an ExpandedNodeId because it could be node in another server. 
                // The ToNodeId function is used to convert a local NodeId stored in a ExpandedNodeId to a NodeId.
                nodes.Add(ExpandedNodeId.ToNodeId(target.TargetId, session.NamespaceUris));
            }

            // return whatever was found.
            return nodes;
        }

        public static ReadValueIdCollection GetReadValueIdCollection(string tag, Session session)
        {

            var RootNode = ObjectIds.ObjectsFolder;
            var sourceNodeId = FindNode(tag, RootNode, session);
            var attributeId = Attributes.Value;
            var readValue = new ReadValueId
            {
                NodeId = sourceNodeId,
                AttributeId = attributeId
            };
            return new ReadValueIdCollection { readValue };
        }
        public static ReadValueIdCollection GetReadValueIdCollection(string tag, uint attributeId, Session session)
        {

            var RootNode = ObjectIds.ObjectsFolder;
            var sourceNodeId = FindNode(tag, RootNode, session);
            var readValue = new ReadValueId
            {
                NodeId = sourceNodeId,
                AttributeId = attributeId
            };
            return new ReadValueIdCollection { readValue };
        }
        /// <summary>
        /// find node
        /// </summary>
        /// <param name="tag"></param>
        /// <param name="nodeId"></param>
        /// <param name="session"></param>
        /// <returns></returns>
        private static NodeId FindNode(string tag, NodeId nodeId, Session session)
        {
            var folders = tag.Split('.');
            var head = folders.FirstOrDefault();
            NodeId found;
            try
            {
                var subNodes = Browse(session, nodeId == null ? ObjectIds.ObjectsFolder : nodeId);
                var subfound = subNodes.Find(n => n.DisplayName == head);

                found =  ExpandedNodeId.ToNodeId(subfound.NodeId, session.NamespaceUris);

            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("The tag \"{0}\" doesn't exist on folder \"{1}\"", head, tag), ex);
            }

            return folders.Length == 1
              ? found // last node, return it
              : FindNode(string.Join(".", folders.Except(new[] { head })), found, session); // find sub nodes
        }
       
      
        private static ReferenceDescriptionCollection Browse(Session session, NodeId nodeId)
        {
            var desc = new BrowseDescription
            {
                NodeId = nodeId,
                BrowseDirection = BrowseDirection.Forward,
                IncludeSubtypes = true,
                NodeClassMask = 0U,
                ResultMask = 63U,
            };
            return Browse(session, desc, true);
        }

        private static ReferenceDescriptionCollection Browse(Session session, BrowseDescription nodeToBrowse, bool throwOnError)
        {
            try
            {
                var descriptionCollection = new ReferenceDescriptionCollection();
                var nodesToBrowse = new BrowseDescriptionCollection { nodeToBrowse };
                BrowseResultCollection results;
                DiagnosticInfoCollection diagnosticInfos;
                session.Browse(null, null, 0U, nodesToBrowse, out results, out diagnosticInfos);
                ClientBase.ValidateResponse(results, nodesToBrowse);
                ClientBase.ValidateDiagnosticInfos(diagnosticInfos, nodesToBrowse);
                while (!StatusCode.IsBad(results[0].StatusCode))
                {
                    for (var index = 0; index < results[0].References.Count; ++index)
                        descriptionCollection.Add(results[0].References[index]);
                    if (results[0].References.Count == 0 || results[0].ContinuationPoint == null)
                        return descriptionCollection;
                    var continuationPoints = new ByteStringCollection();
                    continuationPoints.Add(results[0].ContinuationPoint);
                    session.BrowseNext(null, false, continuationPoints, out results, out diagnosticInfos);
                    ClientBase.ValidateResponse(results, continuationPoints);
                    ClientBase.ValidateDiagnosticInfos(diagnosticInfos, continuationPoints);
                }
                throw new ServiceResultException(results[0].StatusCode);
            }
            catch (Exception ex)
            {
                if (throwOnError)
                    throw new ServiceResultException(ex, 2147549184U);
                return null;
            }
        }


    }


}
