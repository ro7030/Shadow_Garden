namespace ShadowGarden.Presentation
{
    /// <summary>Learning-intent metadata for main stages (Presentation-only; not used by Core rules).</summary>
    public static class MainStageLearningMeta
    {
        public readonly struct Entry
        {
            public string StageId { get; }
            public string LearningGoal { get; }
            public Entry(string stageId, string learningGoal) { StageId = stageId; LearningGoal = learningGoal; }
        }

        public static Entry Get(string stageId)
        {
            foreach (var e in All)
            {
                if (e.StageId == stageId) return e;
            }
            return new Entry(stageId, string.Empty);
        }

        public static readonly Entry[] All =
        {
            new Entry("1-1", "이동, 태양등 접근, Q/E 회전, 단일 그림자 횡단"),
            new Entry("1-2", "낮음 2칸과 높음 4칸, 거리 비교, 안전한 오답 복구"),
            new Entry("1-3", "같은 채널 기둥군의 동시 갱신, 180° 회전, 높이 3종 회상"),
            new Entry("1-4", "두 태양등, 채널 분리, 중첩 위험, 밤꽃 완료"),
            new Entry("2-1", "두 태양등의 접근 순서, 180° 회전, 세로 그림자"),
            new Entry("2-2", "세 태양등 온보딩, 세 구간 조합, 높이 혼합"),
            new Entry("2-3", "태양등 재방문, 상태 전환, 안전한 왕복"),
            new Entry("2-4", "3채널 종합, 높이 3종, 중첩 후보 비교, 밤꽃 완료"),
            new Entry("3-1", "확장 격자 적응, 3채널 회상, 전체 보드 시야"),
            new Entry("3-2", "미끼 경로 판별, 안전 우회, 넓은 보드 시선 회수"),
            new Entry("3-3", "첫 4채널, 네 구역 분할, 누적 상태 확인"),
            new Entry("3-4", "4채널 최종 조합, 높이 3종, 중첩 회피, 확장 격자, 밤꽃 완결"),
        };
    }
}
