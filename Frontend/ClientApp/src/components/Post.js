import React, { Component } from 'react';
import Utils from "../Utils";

export class Post extends Component {
  displayName = Post.name

  constructor(props) {
    super(props);
    this.state = { post: [], loading: true };

    fetch(process.env.REACT_APP_API_ROOT + '/api/posts/' + props.match.params.blogname + '/' + props.match.params.postid)
    .then(response => response.json())
    .then(data => {
      this.setState({ post: data, loading: false });
    });
  }

  static renderPost(post) {
    return (
        <img src={Utils.GetBigPhotoUrl(post)} alt=""/>
    );
  }

  render() {
    let contents = this.state.loading
      ? <p><em>Loading...</em></p>
        : Post.renderPost(this.state.post);

    return (
      <div>
        <h1>{this.props.match.params.blogname}</h1>
        {contents}
      </div>
    );
  }
}
