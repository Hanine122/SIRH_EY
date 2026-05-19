namespace SIRH.EY.Models;

/// <summary>
/// Énumération des statuts possibles pour un collaborateur dans le système RH Insights
/// </summary>
public enum StatutCollaborateur
{
    /// <summary>Collaborateur actif et présent</summary>
    Actif = 0,
    
    /// <summary>Collaborateur en congé (vacances, maladie, etc.)</summary>
    EnConge = 1,
    
    /// <summary>Collaborateur en cours de passation de poste</summary>
    EnPassation = 2,
    
    /// <summary>Poste vacant (sans titulaire actuel)</summary>
    Vacant = 3
}
