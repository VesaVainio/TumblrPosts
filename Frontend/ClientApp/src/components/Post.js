import React, { Component } from 'react';
import Row from "react-bootstrap/Row";
import Utils from "../Utils";
import './Post.css';

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

  static createMarkup(post) {
    return {__html: post.Body};
  }

  static renderPost(post) {
    if (post.Videos && post.Videos.length !== 0) {
      return (
        <div>
          {post.Videos.map(video =>
            <Row>
              <video controls src={video.Url}>
                Video not supported.
              </video>
            </Row>
          )}
        </div>
      );
    } else if (post.Photos && post.Photos.length !== 0) {
      var photos = post.Photos;
      return (
        <div>
        {photos.map(photo =>
          <Row>
            <div class="col photorow">
              <img src={Utils.GetBigPhotoUrl(photo)} alt="" />
            </div>
          </Row>
        )}
        </div>
      );
    } else if (post.Body) {
      return (
        <Row>
          <div dangerouslySetInnerHTML={{__html: post.Body}} />
        </Row>
      )
    }
  }

  render() {
    let contents = this.state.loading
      ? <p><em>Loading...</em></p>
        : Post.renderPost(this.state.post);

    return (
      <div>
      <Row>
        <div class='col'>
          <h1>{this.props.match.params.blogname}</h1>
        </div>
      </Row>
      {contents}
      </div>
    );
  }
}
