#pragma once

#include <Script/ScriptConversionHelpers.h>
#include <add_on/scriptarray/scriptarray.h>
#include <string>
#include <vector>

namespace thrive {

class SpeciesNameController {
public:
    std::vector<std::string> prefixcofixes;
    std::vector<std::string> prefixes_v;
    std::vector<std::string> prefixes_c;
    std::vector<std::string> cofixes_v;
    std::vector<std::string> cofixes_c;
    std::vector<std::string> suffixes;
    std::vector<std::string> suffixes_c;
    std::vector<std::string> suffixes_v;

    CScriptArray*
        getVowelPrefixes();

    CScriptArray*
        getConsonantPrefixes();

    CScriptArray*
        getVowelCofixes();

    CScriptArray*
        getConsonantCofixes();

    CScriptArray*
        getSuffixes();

    CScriptArray*
        getConsonantSuffixes();

    CScriptArray*
        getVowelSuffixes();

    CScriptArray*
        getPrefixCofix();

    SpeciesNameController();

    SpeciesNameController(std::string jsonFilePath);
};

} // namespace thrive
