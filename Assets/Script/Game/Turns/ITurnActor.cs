public interface ITurnActor
{
    // �غϿ�ʼ��ˢ����Դ���� ResetStride��
    void OnTurnStart();
    // ִ�и� Actor �Ļغϣ���ҵȴ����룬������AI��
    System.Collections.IEnumerator TakeTurn();
    // �Ƿ����ж������� Stride>0��
    bool HasPendingAction { get; }
}
