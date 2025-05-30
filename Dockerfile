FROM mcr.microsoft.com/dotnet/runtime-deps:9.0-alpine AS base

ARG TARGETPLATFORM
COPY binaries/ /binaries/

RUN case "$TARGETPLATFORM" in \
        "linux/amd64") cp /binaries/linux-x64/azfwrmgr /bin/azfwrmgr ;; \
        "linux/arm64") cp /binaries/linux-arm64/azfwrmgr /bin/azfwrmgr ;; \
        *) echo "Unsupported: $TARGETPLATFORM" && exit 1 ;; \
    esac

# remove binaries we don't need
RUN rm -rf /binaries

# set executable permissions (needed because we are copying from  outside the container)
RUN chmod +x /bin/azfwrmgr

# Final minimal image.
# We use a multi-stage build to avoid including the entire /binaries directory in the final image.
# If we removed /binaries in the same layer where we copied it, it would still be part of the image history and increase the final size.
# This second stage ensures only the azfwrmgr binary and runtime dependencies are retained.
FROM mcr.microsoft.com/dotnet/runtime-deps:9.0-alpine AS runtime

RUN apk add --no-cache gcompat
COPY --from=base /bin/azfwrmgr /bin/azfwrmgr
ENTRYPOINT ["/bin/azfwrmgr"]
