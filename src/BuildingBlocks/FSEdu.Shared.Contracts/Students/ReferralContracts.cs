namespace FSEdu.Shared.Contracts.Students;

public sealed record MyReferralInfoDto(
    string MyCode,
    string? RedeemedCode,
    int CreditEgp,
    int InvitedCount,
    int RewardedCount,
    int TotalRewardEgp,
    List<MyReferredRowDto> Invited
);

public sealed record MyReferredRowDto(
    string MaskedName,
    DateTime CreatedAtUtc,
    bool Rewarded,
    int RewardEgp
);

public sealed record RedeemReferralCodeRequest(string Code);
