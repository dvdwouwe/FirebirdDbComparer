using System.Collections.Generic;

using FirebirdDbComparer.Common;
using FirebirdDbComparer.Common.Equatable;
using FirebirdDbComparer.Exceptions;
using FirebirdDbComparer.Interfaces;
using FirebirdDbComparer.SqlGeneration;

namespace FirebirdDbComparer.DatabaseObjects.Primitives
{
    public sealed class Role : Primitive<Role>, IHasSystemFlag, IHasDescription
    {
        private static readonly EquatableProperty<Role>[] s_EquatableProperties =
        {
            new EquatableProperty<Role>(x => x.RoleName, nameof(RoleName)),
            new EquatableProperty<Role>(x => x.OwnerName, nameof(OwnerName)),
            new EquatableProperty<Role>(x => x.RoleFlag, nameof(RoleFlag)),
            new EquatableProperty<Role>(x => x.SystemFlag, nameof(SystemFlag))
        };

        public Role(ISqlHelper sqlHelper)
            : base(sqlHelper)
        { }

        public Identifier RoleName { get; private set; }
        public DatabaseStringOrdinal OwnerName { get; private set; }
        public DatabaseStringOrdinal Description { get; private set; }
        private SystemFlagType _SystemFlag { get; set; }

        public RoleFlagType RoleFlag => (RoleFlagType)_SystemFlag;

        public SystemFlagType SystemFlag => (SystemFlagType)((int)_SystemFlag & ~(int)RoleFlag);

        protected override Role Self => this;

        protected override EquatableProperty<Role>[] EquatableProperties => s_EquatableProperties;

        protected override IEnumerable<Command> OnCreate(IMetadata sourceMetadata, IMetadata targetMetadata, IComparerContext context)
        {
            yield return new Command()
                .Append($"CREATE ROLE {RoleName.AsSqlIndentifier()}");
            if (RoleFlag.HasFlag(RoleFlagType.ROLE_FLAG_MAY_TRUST))
            {
                yield return new Command()
                    .Append($"ALTER ROLE {RoleName.AsSqlIndentifier()} SET AUTO ADMIN MAPPING");
            }
        }

        protected override IEnumerable<Command> OnDrop(IMetadata sourceMetadata, IMetadata targetMetadata, IComparerContext context)
        {
            yield return new Command().Append($"DROP ROLE {RoleName.AsSqlIndentifier()}");
        }

        protected override IEnumerable<Command> OnAlter(IMetadata sourceMetadata, IMetadata targetMetadata, IComparerContext context)
        {
            var otherRole = FindOtherChecked(targetMetadata.MetadataRoles.Roles, RoleName, "role");

            if (EquatableHelper.PropertiesEqual(this, otherRole, EquatableProperties, nameof(RoleFlag)))
            {
                yield return new Command()
                    .Append($"ALTER ROLE {RoleName.AsSqlIndentifier()} {(RoleFlag.HasFlag(RoleFlagType.ROLE_FLAG_MAY_TRUST) ? "SET" : "DROP")} AUTO ADMIN MAPPING");
            }
            else
            {
                throw new NotSupportedOnFirebirdException($"Altering role is not supported ({RoleName}).");
            }
        }

        protected override Identifier OnPrimitiveTypeKeyObjectName() => RoleName;

        internal static Role CreateFrom(ISqlHelper sqlHelper, IDictionary<string, object> values)
        {
            var result =
                new Role(sqlHelper)
                {
                    RoleName = new Identifier(sqlHelper, values["RDB$ROLE_NAME"].DbValueToString()),
                    OwnerName = values["RDB$OWNER_NAME"].DbValueToString(),
                    Description = values["RDB$DESCRIPTION"].DbValueToString(),
                    _SystemFlag = (SystemFlagType)values["RDB$SYSTEM_FLAG"].DbValueToInt32().GetValueOrDefault()
                };
            return result;
        }
    }
}