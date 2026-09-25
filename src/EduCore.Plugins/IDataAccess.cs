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

        /// <summary>Number of records matching every equality, not counting the record excludeId.</summary>
        int CountWhere(string entityName, Guid excludeId, params Tuple<string, object>[] equals);

        /// <summary>The first record matching every equality, ordered by orderDescBy descending, or null.</summary>
        Entity FindFirst(string entityName, string[] columns, string orderDescBy, params Tuple<string, object>[] equals);

        /// <summary>Ids of every record matching all equalities, not counting the record excludeId.</summary>
        System.Collections.Generic.IList<Guid> FindIds(string entityName, Guid excludeId, params Tuple<string, object>[] equals);

        /// <summary>Number of records whose setAttribute is one of ids and that match every equality, not counting excludeId.</summary>
        int CountInSet(string entityName, Guid excludeId, string setAttribute, System.Collections.Generic.IList<Guid> ids, params Tuple<string, object>[] equals);

        /// <summary>Every record matching all equalities (up to 5000), with the given columns.</summary>
        System.Collections.Generic.IList<Entity> FindAll(string entityName, string[] columns, params Tuple<string, object>[] equals);
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

        public int CountWhere(string entityName, Guid excludeId, params Tuple<string, object>[] equals)
        {
            var query = Build(entityName, new string[0], null, 2, equals);
            if (excludeId != Guid.Empty) query.Criteria.AddCondition(entityName + "id", ConditionOperator.NotEqual, excludeId);
            return _service.RetrieveMultiple(query).Entities.Count;
        }

        public Entity FindFirst(string entityName, string[] columns, string orderDescBy, params Tuple<string, object>[] equals)
        {
            var query = Build(entityName, columns, orderDescBy, 1, equals);
            var rows = _service.RetrieveMultiple(query).Entities;
            return rows.Count == 0 ? null : rows[0];
        }

        public System.Collections.Generic.IList<Guid> FindIds(string entityName, Guid excludeId, params Tuple<string, object>[] equals)
        {
            var query = Build(entityName, new string[0], null, 5000, equals);
            if (excludeId != Guid.Empty) query.Criteria.AddCondition(entityName + "id", ConditionOperator.NotEqual, excludeId);
            var ids = new System.Collections.Generic.List<Guid>();
            foreach (var e in _service.RetrieveMultiple(query).Entities) ids.Add(e.Id);
            return ids;
        }

        public int CountInSet(string entityName, Guid excludeId, string setAttribute, System.Collections.Generic.IList<Guid> ids, params Tuple<string, object>[] equals)
        {
            if (ids == null || ids.Count == 0) return 0;
            var query = Build(entityName, new string[0], null, 5000, equals);
            var values = new object[ids.Count];
            for (int i = 0; i < ids.Count; i++) values[i] = ids[i];
            query.Criteria.AddCondition(setAttribute, ConditionOperator.In, values);
            if (excludeId != Guid.Empty) query.Criteria.AddCondition(entityName + "id", ConditionOperator.NotEqual, excludeId);
            return _service.RetrieveMultiple(query).Entities.Count;
        }

        public System.Collections.Generic.IList<Entity> FindAll(string entityName, string[] columns, params Tuple<string, object>[] equals)
        {
            return _service.RetrieveMultiple(Build(entityName, columns, null, 5000, equals)).Entities;
        }

        private static QueryExpression Build(string entityName, string[] columns, string orderDescBy, int top, Tuple<string, object>[] equals)
        {
            var query = new QueryExpression(entityName) { ColumnSet = new ColumnSet(columns), TopCount = top };
            foreach (var e in equals) query.Criteria.AddCondition(e.Item1, ConditionOperator.Equal, e.Item2);
            if (orderDescBy != null) query.AddOrder(orderDescBy, OrderType.Descending);
            return query;
        }
    }
}
