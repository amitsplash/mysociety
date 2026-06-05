using MySociety.Application.Members.Dtos;



using MySociety.Domain.Enums;







namespace MySociety.Application.Groups.Dtos;







public record CreateGroupRequest(



    string Name,



    GroupType Type,



    ContributionModel ContributionModel,



    decimal ContributionAmount,



    ContributionFrequency ContributionFrequency,



    decimal OpeningMaintenanceBalance,



    decimal OpeningCorpusBalance = 0,



    decimal CreatorOpeningBalance = 0,



    decimal? CreatorSquareFeet = null,



    decimal CreatorCorpusAmount = 0,



    bool CreatorCorpusPaid = false);







public record UpdateGroupRequest(



    string Name,



    GroupType Type,



    ContributionModel ContributionModel,



    decimal ContributionAmount,



    ContributionFrequency ContributionFrequency);







public record GroupResponse(



    Guid Id,



    string Name,



    GroupType Type,



    ContributionModel ContributionModel,



    decimal ContributionAmount,



    ContributionFrequency ContributionFrequency,



    decimal OpeningMaintenanceBalance,



    decimal OpeningCorpusBalance,



    DateTime CreatedAt);







public record CreateGroupResponse(



    GroupResponse Group,



    MemberResponse CreatorMember);




