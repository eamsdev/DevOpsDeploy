# Question/Assumptions

### Question: If the same release is released multiple times to same environment, should we retain previous unique n-release?

**Working assumption**: Retain previous unique n-release.
**Justification**: This prevents unwanted removal of previous releases in case someone attempts to test the build system by deploying the same release multiple times to the same environment

### Question: If a project no longer exists, but still is referenced by the release and deployment, should it be retained?

**Working assumption**: Do not retain release without project reference.
**Justification**: Because the project no longer exists, assume customer no longer require the release to be retained

### Question: If an environment no longer exists, but still is referenced by the deployment, should it be retained?

**Working assumption**: Do not retain release deployed to environment that no longer exists.
**Justification**: Because the environment no longer exists, assume customer no longer require the release to be retained

# Improvements

1. The requirements specifically call for **deployed** releases to be retained. How about releases that are "ahead" of the deployment? Running the retention rule will not retain these releases, the rule might need to be amended to also retain releases that are ahead of deployment