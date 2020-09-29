# DotNetVueIntegration
Integration Point to run Vue (via Vite server hot reload) in the SPA framework within DOTNET5.

## To get started

1. Create a new ASP.NET 5 SPA Application
1. Remove the `/ClientApp` directory
1. Use `npm init vite` to create a new `vue` app in the `/ClientApp` directory
1. Add `VueHelper.cs` file to your project (Nuget Package coming soon...)
1. Modify Startup.cs
   1. Add the line `spa.UseViteDevelopmentServer();` in it place of the current dev server for the SPA Builder.

Check your .csproj file for the following `<Target>` sections:

*  Remove unnecessary sections (e.g. for Angular or React)
*  Added `<Target Name="BuildVue">` section for release
*  Added `<Target Name="PublishRunVite">` section 

```xml
<Project Sdk="Microsoft.NET.Sdk.Web">

	<PropertyGroup>
		<TargetFramework>net5.0</TargetFramework>
		<TypeScriptCompileBlocked>true</TypeScriptCompileBlocked>
		<TypeScriptToolsVersion>Latest</TypeScriptToolsVersion>
		<IsPackable>false</IsPackable>
		<SpaRoot>ClientApp\</SpaRoot>
		<DefaultItemExcludes>$(DefaultItemExcludes);$(SpaRoot)node_modules\**</DefaultItemExcludes>

		<!-- Set this to true if you enable server-side prerendering -->
		<BuildServerSideRenderer>false</BuildServerSideRenderer>
	</PropertyGroup>

	<ItemGroup>
		<PackageReference Include="Microsoft.AspNetCore.SpaServices.Extensions" Version="5.0.0-rc.1.20451.17" />
	</ItemGroup>

	<ItemGroup>
		<!-- Don't publish the SPA source files, but do show them in the project files list -->
		<Content Remove="$(SpaRoot)**" />
		<None Remove="$(SpaRoot)**" />
		<None Include="$(SpaRoot)**" Exclude="$(SpaRoot)node_modules\**" />
	</ItemGroup>

	<ItemGroup>
		<Folder Include="wwwroot\" />
	</ItemGroup>

	<Target Name="DebugEnsureNodeEnv" BeforeTargets="Build" Condition=" '$(Configuration)' == 'Debug' And !Exists('$(SpaRoot)node_modules') ">
		<!-- Ensure Node.js is installed -->
		<Exec Command="node --version" ContinueOnError="true">
			<Output TaskParameter="ExitCode" PropertyName="ErrorCode" />
		</Exec>
		<Error Condition="'$(ErrorCode)' != '0'" Text="Node.js is required to build and run this project. To continue, please install Node.js from https://nodejs.org/, and then restart your command prompt or IDE." />
		<Message Importance="high" Text="Restoring dependencies using 'npm'. This may take several minutes..." />
		<Exec WorkingDirectory="$(SpaRoot)" Command="npm install" />
	</Target>

	<Target Name="BuildVue" BeforeTargets="Build" Condition=" '$(Configuration)' == 'Release'">
		<!-- Ensure Node.js is installed -->
		<Exec Command="node --version" ContinueOnError="true">
			<Output TaskParameter="ExitCode" PropertyName="ErrorCode" />
		</Exec>
		<Error Condition="'$(ErrorCode)' != '0'" Text="Node.js is required to build and run this project. To continue, please install Node.js from https://nodejs.org/, and then restart your command prompt or IDE." />
		<Message Importance="high" Text="Restoring dependencies using 'npm'. This may take several minutes..." />
		<Exec WorkingDirectory="$(SpaRoot)" Command="npm run build" />
	</Target>

	<Target Name="PublishRunVite" AfterTargets="ComputeFilesToPublish">
		<!-- As part of publishing, ensure the JS resources are freshly built in production mode -->
		<Exec WorkingDirectory="$(SpaRoot)" Command="npm install" />
		<Exec WorkingDirectory="$(SpaRoot)" Command="npm run build" />
		<Exec WorkingDirectory="$(SpaRoot)" Command="npm run build --ssr" Condition=" '$(BuildServerSideRenderer)' == 'true' " />

		<!-- Include the newly-built files in the publish output -->
		<ItemGroup>
			<DistFiles Include="$(SpaRoot)dist\**; $(SpaRoot)dist-server\**" />
			<DistFiles Include="$(SpaRoot)node_modules\**" Condition="'$(BuildServerSideRenderer)' == 'true'" />
			<ResolvedFileToPublish Include="@(DistFiles->'%(FullPath)')" Exclude="@(ResolvedFileToPublish)">
				<RelativePath>%(DistFiles.Identity)</RelativePath>
				<CopyToPublishDirectory>PreserveNewest</CopyToPublishDirectory>
				<ExcludeFromSingleFile>true</ExcludeFromSingleFile>
			</ResolvedFileToPublish>
		</ItemGroup>
	</Target>

</Project>
```

NOTES:

* On first run, the helper will export your local asp.net dev certificate to the Vite Hosted Vue app and create a devcert.pfx file and vite.config.js file.