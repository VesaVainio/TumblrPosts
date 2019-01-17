import React, { Component } from 'react';
import StackGrid from "react-stack-grid";
import Utils from "../Utils";

export class Posts extends Component {
  displayName = Posts.name

  constructor(props) {
    super(props);
    this.state = { post: [], loading: true };

    fetch(process.env.REACT_APP_API_ROOT + '/api/posts/' + props.match.params.blogname)
    .then(response => response.json())
    .then(data => {
      this.setState({ post: data, loading: false });
    });
  }

  static renderPost(posts) {
    return (
        <img src={Utils.GetSmallPhotoUrl(post)}/>
    );
  }

  render() {
    let contents = this.state.loading
      ? <p><em>Loading...</em></p>
        : Posts.renderPost(this.state.post);

    return (
      <div>
        <h1>{this.props.match.params.blogname}</h1>
        {contents}
      </div>
    );
  }
}
