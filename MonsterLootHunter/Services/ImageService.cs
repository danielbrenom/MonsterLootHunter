using Dalamud.Interface.Textures;
using Dalamud.Interface.Textures.TextureWraps;
using Dalamud.Plugin.Services;

namespace MonsterLootHunter.Services;

public class ImageService(ITextureProvider textureProvider)
{
    public IDalamudTextureWrap GetIconTexture(int iconId, bool isHq = false)
    {
        return textureProvider.GetFromGameIcon(new GameIconLookup((uint)iconId, isHq)).GetWrapOrEmpty();
    }
}
