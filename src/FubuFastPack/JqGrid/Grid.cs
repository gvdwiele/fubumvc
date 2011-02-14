﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using FubuFastPack.Domain;
using FubuFastPack.Querying;
using Microsoft.Practices.ServiceLocation;

namespace FubuFastPack.JqGrid
{
    public abstract class Grid<TEntity, TService> : ISmartGrid where TEntity : DomainEntity
    {
        private readonly GridDefinition<TEntity> _definition = new GridDefinition<TEntity>();
        private readonly IList<Action<IDictionary<string, object>>> _colModelModifications 
            = new List<Action<IDictionary<string, object>>>();

        protected Action<IDictionary<string, object>> modifyColumnModel
        {
            set
            {
                _colModelModifications.Add(value);
            }
        }

        public GridResults Invoke(IServiceLocator services, GridDataRequest request)
        {
            var runner = services.GetInstance<IGridRunner<TEntity, TService>>();
            var source = BuildSource(runner.Service);

            return runner.RunGrid(_definition, source, request);
        }

        public IEnumerable<FilteredProperty> AllFilteredProperties(IQueryService queryService)
        {
            // Force the enumerable to execute so we don't keep building new FilteredProperty objects
            var properties = _definition.Columns.SelectMany(x => x.FilteredProperties()).ToList();
            properties.Each(x => x.Operators = queryService.FilterOptionsFor<TEntity>(x.Accessor));
            return properties;
        }

        public IEnumerable<IDictionary<string, object>> ToColumnModel()
        {
            var columns = _definition.Columns.SelectMany(x => x.ToDictionary()).ToList();
            columns.Each(c => _colModelModifications.Each(m => m(c)));

            return columns;
        }

        public IGridDefinition Definition
        {
            get { return _definition; }
        }

        public void SortAscending(Expression<Func<TEntity, object>> property)
        {
            _definition.SortBy = SortRule<TEntity>.Ascending(property);
        }

        public void SortDescending(Expression<Func<TEntity, object>> property)
        {
            _definition.SortBy = SortRule<TEntity>.Descending(property);
        }

        protected void LimitRowsTo(int count)
        {
            _definition.MaxCount = count;
        }

        protected FilterColumn<TEntity> FilterOn(Expression<Func<TEntity, object>> expression)
        {
            return _definition.AddColumn(new FilterColumn<TEntity>(expression));
        }

        protected GridColumn<TEntity> Show(Expression<Func<TEntity, object>> expression)
        {
            return _definition.Show(expression);
        }

        protected LinkColumn<TEntity> ShowViewLink(Expression<Func<TEntity, object>> expression)
        {
            return _definition.ShowViewLink(expression);
        }

        public GridDefinition<TEntity>.OtherEntityLinkExpression<TOther> ShowViewLinkForOther<TOther>(
            Expression<Func<TEntity, TOther>> entityProperty) where TOther : DomainEntity
        {
            return _definition.ShowViewLinkForOther(entityProperty);
        }

        public abstract IGridDataSource<TEntity> BuildSource(TService service);

        public void DoNotAllowUserSorting()
        {
            modifyColumnModel = dict =>
            {
                if (dict.ContainsKey("sortable"))
                {
                    dict["sortable"] = false;
                }
                else
                {
                    dict.Add("sortable", false);
                }
            };
        }
    }
}