﻿using System;
using System.Data.Linq;
using System.Data.Linq.Mapping;
using System.Linq;
using Remotion.Data.Linq.LinqToSqlAdapter;

namespace Remotion.Data.Linq.IntegrationTests.TestDomain.Northwind
{
  public class RelinqNorthwindDataProvider : INorthwindDataProvider
  {
    private readonly IConnectionManager _manager;
    private readonly MappingResolver _resolver;
    private readonly IQueryResultRetriever _retriever;
    private readonly IQueryExecutor _executor;

    public RelinqNorthwindDataProvider ()
    {
      _manager = new NorthwindConnectionManager ();
      _resolver = new MappingResolver (new AttributeMappingSource().GetModel (typeof (NorthwindDataContext)));
      _retriever = new QueryResultRetriever (_manager, _resolver);
      _executor = new QueryExecutor (_retriever, _resolver);
    }

    public IQueryable<Product> Products
    {
      get { return CreateQueryable<Product>(); }
    }

    public IQueryable<Customer> Customers
    {
      get { return CreateQueryable<Customer> (); }
    }

    public IQueryable<Employee> Employees
    {
      get { return CreateQueryable<Employee> (); }
    }

    public IQueryable<Category> Categories
    {
      get { return CreateQueryable<Category> (); }
    }

    public IQueryable<Order> Orders
    {
      get { return CreateQueryable<Order> (); }
    }

    public IQueryable<OrderDetail> OrderDetails
    {
      get { return CreateQueryable<OrderDetail> (); }
    }

    public IQueryable<Contact> Contacts
    {
      get { return CreateQueryable<Contact> (); }
    }

    public IQueryable<Invoices> Invoices
    {
      get { return CreateQueryable<Invoices> (); }
    }

    public IQueryable<QuarterlyOrder> QuarterlyOrders
    {
      get { return CreateQueryable<QuarterlyOrder> (); }
    }

    public IQueryable<Shipper> Shippers
    {
      get { return CreateQueryable<Shipper> (); }
    }

    public IQueryable<Supplier> Suppliers
    {
      get { return CreateQueryable<Supplier> (); }
    }

    public decimal? TotalProductUnitPriceByCategory (int categoryID)
    {
      throw new NotImplementedException ("Stored procedures are not relevant for the re-linq SQL backend integration tests.");
    }

    public decimal? MinUnitPriceByCategory (int? nullable)
    {
      throw new NotImplementedException ("Stored procedures are not relevant for the re-linq SQL backend integration tests.");
    }

    public IQueryable<ProductsUnderThisUnitPriceResult> ProductsUnderThisUnitPrice (decimal @decimal)
    {
      throw new NotImplementedException ("Stored procedures are not relevant for the re-linq SQL backend integration tests.");
    }

    public int CustomersCountByRegion (string wa)
    {
      throw new NotImplementedException ("Stored procedures are not relevant for the re-linq SQL backend integration tests.");
    }

    public ISingleResult<CustomersByCityResult> CustomersByCity (string london)
    {
      throw new NotImplementedException ("Stored procedures are not relevant for the re-linq SQL backend integration tests.");
    }

    public IMultipleResults WholeOrPartialCustomersSet (int p0)
    {
      throw new NotImplementedException ("Stored procedures are not relevant for the re-linq SQL backend integration tests.");
    }

    public IMultipleResults GetCustomerAndOrders (string seves)
    {
      throw new NotImplementedException ("Stored procedures are not relevant for the re-linq SQL backend integration tests.");
    }

    public void CustomerTotalSales (string customerID, ref decimal? totalSales)
    {
      throw new NotImplementedException ("Stored procedures are not relevant for the re-linq SQL backend integration tests.");
    }

    private IQueryable<T> CreateQueryable<T> ()
    {
      return new QueryableAdapter<T> (_executor);
    }
  }
}
