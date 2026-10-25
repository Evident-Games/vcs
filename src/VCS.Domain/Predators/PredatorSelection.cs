using VCS.Domain.Characters;

namespace VCS.Domain.Predators;

public sealed record PredatorSelection(string Id, string DisciplineKey, string Discipline, string SkillKey,
    string Skill, string Specialization, bool SpecializationAdded, TraitValue[] Grants, int HumanityDelta, int BloodDelta)
{
    public bool Equals(PredatorSelection? other) => other is not null && Id == other.Id &&
        DisciplineKey == other.DisciplineKey && Discipline == other.Discipline && SkillKey == other.SkillKey &&
        Skill == other.Skill && Specialization == other.Specialization && SpecializationAdded == other.SpecializationAdded &&
        HumanityDelta == other.HumanityDelta && BloodDelta == other.BloodDelta &&
        Grants is not null && other.Grants is not null && Grants.SequenceEqual(other.Grants);
    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(Id); hash.Add(DisciplineKey); hash.Add(Discipline); hash.Add(SkillKey); hash.Add(Skill);
        hash.Add(Specialization); hash.Add(SpecializationAdded); hash.Add(HumanityDelta); hash.Add(BloodDelta);
        foreach (var grant in Grants) hash.Add(grant);
        return hash.ToHashCode();
    }
}
