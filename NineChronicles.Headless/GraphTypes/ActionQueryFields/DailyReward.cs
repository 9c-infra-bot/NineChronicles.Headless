using GraphQL;
using GraphQL.Types;
using Libplanet;
using Libplanet.Explorer.GraphTypes;
using Nekoyume.Action;
using NCAction = Libplanet.Action.PolymorphicAction<Nekoyume.Action.ActionBase>;

namespace NineChronicles.Headless.GraphTypes;

public partial class ActionQuery
{
    private void RegisterDailyReward()
    {
        Field<NonNullGraphType<ByteStringType>>(
            name: "dailyReward",
            arguments: new QueryArguments(
                new QueryArgument<NonNullGraphType<AddressType>>
                {
                    Name = "avatarAddress",
                    Description = "Avatar address to get daily reward"
                }
            ),
            resolve: context =>
            {
                var avatarAddress = context.GetArgument<Address>("avatarAddress");
                NCAction action = new DailyReward
                {
                    avatarAddress = avatarAddress
                };
                return Encode(context, action);
            }
        );
    }
}