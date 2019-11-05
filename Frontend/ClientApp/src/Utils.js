const Utils = {
  GetSmallPhotoUrl: function(post) {
    const photo = post.Photos[0];
    photo.Sizes.sort((a, b) => a.Nominal - b.Nominal);
    let size = photo.Sizes[0];
    if (size.Nominal === 250 && photo.Sizes.length > 1) {
      size = photo.Sizes[1];
    }
    const base = process.env.REACT_APP_BLOB_ROOT;
    const url = base + "/" + size.Container + "/" + photo.Name + "_" + size.Nominal + "." + photo.Extension;
    return url;
  },

  GetBigPhotoUrl: function(photo) {
    photo.Sizes.sort((a, b) => b.Nominal - a.Nominal);
    let size = photo.Sizes[0];
    const base = process.env.REACT_APP_BLOB_ROOT;
    const url = base + "/" + size.Container + "/" + photo.Name + "_" + size.Nominal + "." + photo.Extension;
    return url;
  }
};

export default Utils;