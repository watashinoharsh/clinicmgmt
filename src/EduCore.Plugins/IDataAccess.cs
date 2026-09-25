using System;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace EduCore.Plugins
{
    /// <summary>Minimal data access used by the rules, so they can be tested without Dataverse.</summary>
    public interface IDataAccess
    {
        /// <summary>Retrieve a record with the given columns, or null when it does not exist.</summary>
        Entity Retrieve(string entityName, Guid id, params string[] columns);

        /// <summary>True when at least one record of entityName has lookupAttribute equal to id.</summary>
        bool Exists(string entityName, string lookupAttribute, Guid id);

        /// <summary>Number of records of entityName whose attribute equals value, not counting the record excludeId.</summary>
        int CountOthers(string entityName, string attribute, object value, Guid excludeId);
    }

    public sealed class OrgDataAccess : IDataAccess
    {
        private readonly IOrganizationService _service;

        public OrgDataAccess(IOrganizationService service)
        {
            if (service == null) throw new ArgumentNullException("service");
            _service = service;
        }

        public Entity Retrieve(string entityName, Guid id, params string[] columns)
        {
            try
            {
                return _service.Retrieve(entityName, id, new ColumnSet(columns));
            }
            catch (System.ServiceModel.FaultException<OrganizationServiceFault>)
            {
                return null;
            }
        }

        public bool Exists(string entityName, string lookupAttribute, Guid id)
        {
            var query = new QueryExpression(entityName)
            {
                ColumnSet = new ColumnSet(false),
                TopCount = 1
            };
            query.Criteria.AddCondition(lookupAttribute, ConditionOperator.Equal, id);
            return _service.RetrieveMultiple(query).Entities.Count > 0;
        }

        public int CountOthers(string entityName, string attribute, object value, Guid excludeId)
        {
            var query = new QueryExpression(entityName)
            {
                ColumnSet = new ColumnSet(false),
                TopCount = 2
            };
            query.Criteria.AddCondition(attribute, ConditionOperator.Equal, value);
            if (excludeId != Guid.Empty) query.Criteria.AddCondition(entityName + "id", ConditionOperator.NotEqual, excludeId);
            return _service.RetrieveMultiple(query).Entities.Count;
        }
    }
}
