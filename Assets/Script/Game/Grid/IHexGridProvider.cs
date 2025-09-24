using Game.Battle;
using Game.Common;
using System.Collections.Generic;

namespace Game.Grid
{
    // �κΡ�������������ʱ����ʵ������BattleHexGrid���Ժ� WorldHexGrid Ҳ�У�
    public interface IHexGridProvider
    {
        uint Version { get; }                 // ����汾���仯ʱ�� Unit �ؽ�����
        GridRecipe recipe { get; }            // �����ȵȲ��������� ScriptableObject��
        IEnumerable<TileTag> EnumerateTiles();// �������� TileTag�������أ�
    }
}
