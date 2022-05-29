// This file is part of the re-linq project (relinq.codeplex.com)
// Copyright (c) rubicon IT GmbH, www.rubicon.eu
// 
// re-linq is free software; you can redistribute it and/or modify it under 
// the terms of the GNU Lesser General Public License as published by the 
// Free Software Foundation; either version 2.1 of the License, 
// or (at your option) any later version.
// 
// re-linq is distributed in the hope that it will be useful, 
// but WITHOUT ANY WARRANTY; without even the implied warranty of 
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the 
// GNU Lesser General Public License for more details.
// 
// You should have received a copy of the GNU Lesser General Public License
// along with re-linq; if not, see http://www.gnu.org/licenses.
// 

using System;
using System.Data.Linq.Mapping;
using Moq;
using NUnit.Framework;

namespace Remotion.Linq.LinqToSqlAdapter.UnitTests
{
  [TestFixture]
  public class MetaDataMemberComparerTest
  {
    [Test]
    public void Equals ()
    {
      const string name = "equal";

      var metaDataMember1 = new Mock<MetaDataMember>();
      metaDataMember1.Setup (dataMember => dataMember.MappedName).Returns (name);

      var metaDataMember2 = new Mock<MetaDataMember>();
      metaDataMember2.Setup (dataMember => dataMember.MappedName).Returns (name);

      var comparer = new MetaDataMemberComparer();

      Assert.That (comparer.Equals (metaDataMember1.Object, metaDataMember2.Object), Is.True);
    }

    [Test]
    public void Equals_ShouldReturnFalse ()
    {
      const string name = "equal";
      const string otherName = "notequal";

      var metaDataMember1 = new Mock<MetaDataMember>();
      metaDataMember1.Setup (dataMember => dataMember.MappedName).Returns (name);

      var metaDataMember2 = new Mock<MetaDataMember>();
      metaDataMember2.Setup (dataMember => dataMember.MappedName).Returns (otherName);

      var comparer = new MetaDataMemberComparer();

      Assert.That (comparer.Equals (metaDataMember1.Object, metaDataMember2.Object), Is.False);
    }
  }
}