#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
apply_mcp_fixes.py

Idempotently re-apply MCP plugin fixes:
- Ensure lightweight DllExport attribute exists under DotNetPlugin.Stub/Attributes.DllExport.cs
- Fully qualify DllExport usage in DotNetPlugin.Stub/PluginMain.cs
- Remove RGiesecke.DllExport.Metadata package reference from DotNetPlugin.Stub.csproj
- Make DotNetPlugin.Impl.csproj PostBuild copy platform-aware and safe
- Ensure CI workflows exist or updated for x86/x64 builds with expected artifact names

Run:
  python apply_mcp_fixes.py
"""
import os
import re
from pathlib import Path

REPO_ROOT = Path(__file__).resolve().parent

FILE_IMPL_CSPROJ = REPO_ROOT / "DotNetPlugin.Impl" / "DotNetPlugin.Impl.csproj"
FILE_STUB_CSPROJ = REPO_ROOT / "DotNetPlugin.Stub" / "DotNetPlugin.Stub.csproj"
FILE_PLUGIN_MAIN = REPO_ROOT / "DotNetPlugin.Stub" / "PluginMain.cs"
FILE_DLL_EXPORT_ATTR = REPO_ROOT / "DotNetPlugin.Stub" / "Attributes.DllExport.cs"
FILE_WF_X86 = REPO_ROOT / ".github" / "workflows" / "build-x86.yml"
FILE_WF_X64 = REPO_ROOT / ".github" / "workflows" / "build-x64.yml"

POSTBUILD_TARGET = (
    "  <Target Name=\"PostBuild\" AfterTargets=\"PostBuildEvent\">\n"
    "    <!-- You can update the path to x96dbg here -->\n"
    "    <PropertyGroup>\n"
    "      <X96DbgRootPath>C:\\Users\\User\\Desktop\\x96\\release</X96DbgRootPath>\n"
    "    </PropertyGroup>\n\n"
    "    <Exec Condition=\"'$(Platform)'=='x64' AND Exists('$(X96DbgRootPath)')\" Command=\"xcopy /Y /I &quot;$(TargetDir)*.*&quot; &quot;$(X96DbgRootPath)\\x64\\plugins\\x64DbgMCPServer&quot;\" />\n"
    "    <Exec Condition=\"'$(Platform)'=='x86' AND Exists('$(X96DbgRootPath)')\" Command=\"xcopy /Y /I &quot;$(TargetDir)*.*&quot; &quot;$(X96DbgRootPath)\\x32\\plugins\\x64DbgMCPServer&quot;\" />\n"
    "  </Target>\n"
)

DLL_EXPORT_ATTR_SOURCE = (
    "namespace RGiesecke.DllExport\n"
    "{\n"
    "\tusing System;\n"
    "\tusing System.Runtime.InteropServices;\n\n"
    "\t[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]\n"
    "\tpublic sealed class DllExportAttribute : Attribute\n"
    "\t{\n"
    "\t\tpublic DllExportAttribute()\n"
    "\t\t{\n"
    "\t\t}\n\n"
    "\t\tpublic DllExportAttribute(string entryPoint)\n"
    "\t\t{\n"
    "\t\t\tthis.EntryPoint = entryPoint;\n"
    "\t\t}\n\n"
    "\t\tpublic DllExportAttribute(string entryPoint, CallingConvention callingConvention)\n"
    "\t\t{\n"
    "\t\t\tthis.EntryPoint = entryPoint;\n"
    "\t\t\tthis.CallingConvention = callingConvention;\n"
    "\t\t}\n\n"
    "\t\tpublic string EntryPoint { get; }\n"
    "\t\tpublic CallingConvention CallingConvention { get; }\n"
    "\t}\n"
    "}\n"
)

WF_X86 = (
    "name: Build x86 Plugin\n\n"
    "on:\n"
    "  workflow_dispatch:\n"
    "  push:\n"
    "    paths:\n"
    "      - '**/*.cs'\n"
    "      - '**/*.csproj'\n"
    "      - 'Directory.Build.props'\n"
    "      - '.github/workflows/build-x86.yml'\n\n"
    "jobs:\n"
    "  build:\n"
    "    runs-on: windows-latest\n"
    "    env:\n"
    "      NUGET_PACKAGES: ${{ github.workspace }}\\\\packages\n"
    "    steps:\n"
    "      - name: Checkout\n"
    "        uses: actions/checkout@v4\n\n"
    "      - name: Setup MSBuild\n"
    "        uses: microsoft/setup-msbuild@v2\n\n"
    "      - name: Setup .NET SDK\n"
    "        uses: actions/setup-dotnet@v4\n"
    "        with:\n"
    "          dotnet-version: '8.0.x'\n\n"
    "      - name: Restore\n"
    "        run: msbuild x64DbgMCPServer.sln /t:Restore /p:Platform=x86 /p:Configuration=Release /p:RestorePackagesPath=\"${{ env.NUGET_PACKAGES }}\" /p:BaseIntermediateOutputPath=.cache\\\\obj\\\\\n\n"
    "      - name: Build x86 Release\n"
    "        run: msbuild x64DbgMCPServer.sln /t:Rebuild /p:Platform=x86 /p:Configuration=Release /p:RestorePackagesPath=\"${{ env.NUGET_PACKAGES }}\" /p:BaseIntermediateOutputPath=.cache\\\\obj\\\\ /p:BaseOutputPath=.cache\\\\bin\\\\\n\n"
    "      - name: Upload artifact (dp32)\n"
    "        uses: actions/upload-artifact@v4\n"
    "        with:\n"
    "          name: AgentSmithers_x64DbgMCP_x32Plugin\n"
    "          path: |\n"
    "            bin\\\\x86\\\\Release\\\\**\\\\*.dp32\n"
    "            bin\\\\x86\\\\Release\\\\**\\\\*.dll\n"
    "            bin\\\\x86\\\\Release\\\\**\\\\*.pdb\n"
    "          if-no-files-found: error\n\n"
    "      - name: Cleanup caches\n"
    "        if: always()\n"
    "        run: |\n"
    "          Remove-Item -Recurse -Force .cache -ErrorAction SilentlyContinue\n"
    "          Remove-Item -Recurse -Force packages -ErrorAction SilentlyContinue\n"
)

WF_X64 = (
    "name: Build x64 Plugin\n\n"
    "on:\n"
    "  workflow_dispatch:\n"
    "  push:\n"
    "    paths:\n"
    "      - '**/*.cs'\n"
    "      - '**/*.csproj'\n"
    "      - 'Directory.Build.props'\n"
    "      - '.github/workflows/build-x64.yml'\n\n"
    "jobs:\n"
    "  build:\n"
    "    runs-on: windows-latest\n"
    "    env:\n"
    "      NUGET_PACKAGES: ${{ github.workspace }}\\\\packages\n"
    "    steps:\n"
    "      - name: Checkout\n"
    "        uses: actions/checkout@v4\n\n"
    "      - name: Setup MSBuild\n"
    "        uses: microsoft/setup-msbuild@v2\n\n"
    "      - name: Setup .NET SDK\n"
    "        uses: actions/setup-dotnet@v4\n"
    "        with:\n"
    "          dotnet-version: '8.0.x'\n\n"
    "      - name: Restore\n"
    "        run: msbuild x64DbgMCPServer.sln /t:Restore /p:Platform=x64 /p:Configuration=Release /p:RestorePackagesPath=\"${{ env.NUGET_PACKAGES }}\" /p:BaseIntermediateOutputPath=.cache\\\\obj\\\\\n\n"
    "      - name: Build x64 Release\n"
    "        run: msbuild x64DbgMCPServer.sln /t:Rebuild /p:Platform=x64 /p:Configuration=Release /p:RestorePackagesPath=\"${{ env.NUGET_PACKAGES }}\" /p:BaseIntermediateOutputPath=.cache\\\\obj\\\\ /p:BaseOutputPath=.cache\\\\bin\\\\\n\n"
    "      - name: Upload artifact (dp64)\n"
    "        uses: actions/upload-artifact@v4\n"
    "        with:\n"
    "          name: AgentSmithers_x64DbgMCP_x64Plugin\n"
    "          path: |\n"
    "            bin\\\\x64\\\\Release\\\\**\\\\*.dp64\n"
    "            bin\\\\x64\\\\Release\\\\**\\\\*.dll\n"
    "            bin\\\\x64\\\\Release\\\\**\\\\*.pdb\n"
    "          if-no-files-found: error\n\n"
    "      - name: Cleanup caches\n"
    "        if: always()\n"
    "        run: |\n"
    "          Remove-Item -Recurse -Force .cache -ErrorAction SilentlyContinue\n"
    "          Remove-Item -Recurse -Force packages -ErrorAction SilentlyContinue\n"
)


def read_text(path: Path) -> str:
    return path.read_text(encoding="utf-8") if path.exists() else ""


def write_text(path: Path, content: str) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(content, encoding="utf-8")


def update_impl_csproj() -> None:
    txt = read_text(FILE_IMPL_CSPROJ)
    if not txt:
        print(f"[skip] Missing {FILE_IMPL_CSPROJ}")
        return
    # Replace entire PostBuild target block or insert before closing Project
    postbuild_pattern = re.compile(r"\s*<Target\s+Name=\"PostBuild\"[\s\S]*?</Target>")
    if postbuild_pattern.search(txt):
        txt2 = postbuild_pattern.sub("\n" + POSTBUILD_TARGET.rstrip() + "\n", txt)
    else:
        # Insert before final closing tag
        insert_idx = txt.rfind("</Project>")
        if insert_idx != -1:
            txt2 = txt[:insert_idx] + POSTBUILD_TARGET + txt[insert_idx:]
        else:
            txt2 = txt
    if txt2 != txt:
        write_text(FILE_IMPL_CSPROJ, txt2)
        print(f"[ok] Updated PostBuild in {FILE_IMPL_CSPROJ}")
    else:
        print(f"[ok] PostBuild already up-to-date in {FILE_IMPL_CSPROJ}")


def update_pluginmain_attributes() -> None:
    txt = read_text(FILE_PLUGIN_MAIN)
    if not txt:
        print(f"[skip] Missing {FILE_PLUGIN_MAIN}")
        return
    # Replace [DllExport( with fully qualified
    new_txt = re.sub(r"\[(?:RGiesecke\.DllExport\.)?DllExport\(", "[RGiesecke.DllExport.DllExport(", txt)
    if new_txt != txt:
        write_text(FILE_PLUGIN_MAIN, new_txt)
        print(f"[ok] Qualified DllExport in {FILE_PLUGIN_MAIN}")
    else:
        print(f"[ok] DllExport usage already qualified in {FILE_PLUGIN_MAIN}")


def ensure_dll_export_attribute() -> None:
    content = DLL_EXPORT_ATTR_SOURCE
    current = read_text(FILE_DLL_EXPORT_ATTR)
    if current.strip() != content.strip():
        write_text(FILE_DLL_EXPORT_ATTR, content)
        print(f"[ok] Wrote {FILE_DLL_EXPORT_ATTR}")
    else:
        print(f"[ok] {FILE_DLL_EXPORT_ATTR} already present")


def update_stub_csproj() -> None:
    txt = read_text(FILE_STUB_CSPROJ)
    if not txt:
        print(f"[skip] Missing {FILE_STUB_CSPROJ}")
        return
    # Remove RGiesecke.DllExport.Metadata package reference if present
    txt2 = re.sub(r"\s*<PackageReference\s+Include=\"RGiesecke\.DllExport\.Metadata\"[\s\S]*?</PackageReference>", "", txt)
    if txt2 != txt:
        write_text(FILE_STUB_CSPROJ, txt2)
        print(f"[ok] Removed RGiesecke.DllExport.Metadata from {FILE_STUB_CSPROJ}")
    else:
        print(f"[ok] No RGiesecke.DllExport.Metadata reference in {FILE_STUB_CSPROJ}")


def ensure_workflows() -> None:
    # x86
    wf = read_text(FILE_WF_X86)
    if wf.strip() != WF_X86.strip():
        write_text(FILE_WF_X86, WF_X86)
        print(f"[ok] Updated {FILE_WF_X86}")
    else:
        print(f"[ok] {FILE_WF_X86} already up-to-date")
    # x64
    wf = read_text(FILE_WF_X64)
    if wf.strip() != WF_X64.strip():
        write_text(FILE_WF_X64, WF_X64)
        print(f"[ok] Updated {FILE_WF_X64}")
    else:
        print(f"[ok] {FILE_WF_X64} already up-to-date")


def main() -> None:
    update_impl_csproj()
    update_pluginmain_attributes()
    ensure_dll_export_attribute()
    update_stub_csproj()
    ensure_workflows()
    print("\nDone. Re-run your CI workflows to rebuild artifacts.")

if __name__ == "__main__":
    main()

