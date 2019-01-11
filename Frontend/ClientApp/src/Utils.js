const Utils = {
    GetPhotoUrl: function(post) {
        let photo = post.Photos[0];
        photo.Sizes.sort((a, b) => a.Nominal - b.Nominal);
        let size = photo.Sizes[0];
        let base = process.env.REACT_APP_BLOB_ROOT;
        let url = base + "/" + size.Container + "/" + photo.Name + "_" + size.Nominal + "." + photo.Extension;
        return url;
    }
}

export default Utils;