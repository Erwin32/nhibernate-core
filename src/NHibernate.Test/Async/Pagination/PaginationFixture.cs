﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by AsyncGenerator.
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------


using System;
using System.Collections;
using NHibernate.Cfg;
using NHibernate.Criterion;
using NHibernate.Dialect;
using NUnit.Framework;
using Environment=NHibernate.Cfg.Environment;

namespace NHibernate.Test.Pagination
{
	using System.Threading.Tasks;
	[TestFixture]
	public class PaginationFixtureAsync : TestCase
	{
		protected override string MappingsAssembly
		{
			get { return "NHibernate.Test"; }
		}

		protected override string[] Mappings
		{
			get { return new[] {"Pagination.DataPoint.hbm.xml"}; }
		}

		protected override void Configure(Configuration configuration)
		{
			cfg.SetProperty(Environment.DefaultBatchFetchSize, "20");
		}

		protected override string CacheConcurrencyStrategy
		{
			get { return null; }
		}

		[Test]
		public async Task PagTestAsync()
		{
			using(ISession s = OpenSession())
			using (ITransaction t = s.BeginTransaction())
			{
				for (int i = 0; i < 10; i++)
				{
					var dp = new DataPoint {X = (i * 0.1d)};
					dp.Y = Math.Cos(dp.X);
					await (s.PersistAsync(dp));
				}
				await (t.CommitAsync());
			}

			using(ISession s = OpenSession())
			using (ITransaction t = s.BeginTransaction())
			{
				int size =
					(await (s.CreateSQLQuery("select Id, xval, yval, Description from DataPoint order by xval, yval").AddEntity(
						typeof (DataPoint)).SetMaxResults(5).ListAsync())).Count;
				Assert.That(size, Is.EqualTo(5));
				size = (await (s.CreateQuery("from DataPoint dp order by dp.X, dp.Y").SetFirstResult(5).SetMaxResults(2).ListAsync())).Count;
				Assert.That(size, Is.EqualTo(2));
				size =
					(await (s.CreateCriteria(typeof (DataPoint)).AddOrder(Order.Asc("X")).AddOrder(Order.Asc("Y")).SetFirstResult(8).ListAsync<DataPoint>())).
						Count;
				Assert.That(size, Is.EqualTo(2));
				size =
					(await (s.CreateCriteria(typeof(DataPoint)).AddOrder(Order.Asc("X")).AddOrder(Order.Asc("Y")).SetMaxResults(10).SetFirstResult(8).ListAsync<DataPoint>())).
						Count;
				Assert.That(size, Is.EqualTo(2));
				await (t.CommitAsync());
			}

			using(ISession s = OpenSession())
			using (ITransaction t = s.BeginTransaction())
			{
				await (s.DeleteAsync("from DataPoint"));
				await (t.CommitAsync());
			}
		}

		[Test]
		public async Task PagingWithLock_NH2255Async()
		{
			if (Dialect is Oracle8iDialect)
				Assert.Ignore(@"Oracle does not support row_limiting_clause with for_update_clause
See: https://docs.oracle.com/database/121/SQLRF/statements_10002.htm#BABHFGAA");

			using (ISession s = OpenSession())
			using (ITransaction t = s.BeginTransaction())
			{
				await (s.SaveAsync(new DataPoint() { X = 4 }));
				await (s.SaveAsync(new DataPoint() { X = 5 }));
				await (s.SaveAsync(new DataPoint() { X = 6 }));
				await (s.SaveAsync(new DataPoint() { X = 7 }));
				await (s.SaveAsync(new DataPoint() { X = 8 }));
				await (t.CommitAsync());
			}

			using (ISession s = OpenSession())
			{
				var points =
					await (s.CreateCriteria<DataPoint>()
						.Add(Restrictions.Gt("X", 4.1d))
						.AddOrder(Order.Asc("X"))
						.SetLockMode(LockMode.Upgrade)
						.SetFirstResult(1)
						.SetMaxResults(2)
						.ListAsync<DataPoint>());

				Assert.That(points.Count, Is.EqualTo(2));
				Assert.That(points[0].X, Is.EqualTo(6d));
				Assert.That(points[1].X, Is.EqualTo(7d));
			}

			using (ISession s = OpenSession())
			using (ITransaction t = s.BeginTransaction())
			{
				await (s.CreateQuery("delete from DataPoint").ExecuteUpdateAsync());
				await (t.CommitAsync());
			}
		}
	}
}
