public enum Item {
    MC_Diamond,
    MC_Iron,
}

public static class ItemHelper {
    public static Dictionary<Item, string> ItemMap = new() {
        { Item.MC_Diamond, "minecraft:diamond" },
        { Item.MC_Iron, "minecraft:iron" },
    };
}
