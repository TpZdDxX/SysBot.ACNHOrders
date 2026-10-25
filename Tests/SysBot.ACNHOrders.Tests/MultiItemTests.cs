using System;
using System.Linq;
using Xunit;
using NHSE.Core;

namespace SysBot.ACNHOrders.Tests
{
    public static class MultiItemTests
    {
        [Theory]
        [InlineData("lucky cat", 131)]
        [InlineData("(DIY recipe)", 0x00000295000016A2)]
        [InlineData("Aran-knit sweater (White)", 7672)]
        [InlineData("impish wings (Black)", 0x3464)]
        public static void TestDuplicateVariation(string name, ulong itemValueParse)
        {
            var items = ItemParser.GetItemsFromUserInput(itemValueParse.ToString("X"), new DropBotConfig(), ItemDestination.PlayerDropped);
            Assert.Single(items);
            
            var currentItem = items.ElementAt(0);
            var multiItem = new MultiItem(new Item[1] { currentItem });

            var itemName = GameInfo.Strings.GetItemName(currentItem);
            Assert.StartsWith(name, itemName);

            // variations
            var remake = ItemRemakeUtil.GetRemakeIndex(currentItem.ItemId);
            if (remake > 0)
            {
                var info = ItemRemakeInfoData.List[remake];
                var body = info.GetBodySummary(GameInfo.Strings);
                var bodyVariations = body.Split(new string[2] { "\n", "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
                int varCount = bodyVariations.Length;
                if (!bodyVariations[0].StartsWith("0"))
                    varCount++;

                Assert.Equal(currentItem.ItemId, multiItem.ItemArray.Items[0].ItemId);
                Assert.Equal(1, multiItem.ItemArray.Items[1].Count);
                Assert.Equal(currentItem.ItemId, multiItem.ItemArray.Items[varCount].ItemId);
                Assert.Equal(0, multiItem.ItemArray.Items[varCount].Count);

                foreach (var itm in multiItem.ItemArray.Items)
                {
                    // No leaking associated items
                    if (itm.IsNone)
                        continue;
                    var itmName = GameInfo.Strings.GetItemName(itm);
                    Assert.Equal(itmName, itemName);
                }
            }

            // association
            var associated = GameInfo.Strings.GetAssociatedItems(currentItem.ItemId, out var itemPrefix);
            if (associated.Count > 1 && currentItem.ItemId != Item.DIYRecipe)
            {
                foreach (var asoc in multiItem.ItemArray.Items)
                {
                    var asocName = GameInfo.Strings.GetItemName(asoc);
                    Assert.EndsWith(")", asocName);
                }
            }

            // everything else
            if (currentItem.ItemId == Item.DIYRecipe)
                foreach (var itm in multiItem.ItemArray.Items)
                    Assert.Equal(Item.DIYRecipe, itm.ItemId);
        }
    }
}
