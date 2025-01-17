﻿using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace DeltaKustoLib.CommandModel.Policies
{
    public abstract class EntityPolicyCommandBase : PolicyCommandBase
    {
        public EntityType EntityType { get; }

        public EntityName EntityName { get; }

        public EntityPolicyCommandBase(
            EntityType entityType,
            EntityName entityName,
            JsonDocument policy)
            : base(policy)
        {
            if (entityType != EntityType.Database && entityType != EntityType.Table)
            {
                throw new NotSupportedException(
                    $"Entity type {entityType} isn't supported in this context");
            }
            EntityType = entityType;
            EntityName = entityName;
        }

        public EntityPolicyCommandBase(EntityType entityType, EntityName entityName)
            : this(entityType, entityName, ToJsonDocument(new object()))
        {
        }

        public override string SortIndex =>
            $"{(EntityType == EntityType.Database ? 0 : 1)}_{EntityName}";

        public override bool Equals(CommandBase? other)
        {
            var otherPolicy = other as EntityPolicyCommandBase;
            var areEqualed = otherPolicy != null
                && base.Equals(other)
                && otherPolicy.EntityType.Equals(EntityType)
                && (EntityType == EntityType.Database || otherPolicy.EntityName.Equals(EntityName));

            return areEqualed;
        }
    }
}