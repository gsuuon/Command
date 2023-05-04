ls packages/
| each {|pkg|
  dotnet nuget push $pkg.name --api-key $env.NUGET_API_KEY --source https://api.nuget.org/v3/index.json --skip-duplicate
}
